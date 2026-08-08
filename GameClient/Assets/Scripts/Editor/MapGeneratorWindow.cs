using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// 맵 생성기 (에디터 전용). Assets/Editor/ 폴더에 넣을 것.
// 메뉴: Tools > Map Generator
//
// "혹한 정착지 — 맵 파일 규격" 준수 (blocked 삭제판) + 군락형 자원 배치:
//  - 자원 금지 구역: 모닥불 빛 반경 × 배율(기본 1.75) 안쪽엔 잔가지 3~5개만.
//    나무·돌·식량은 스폰하지 않음
//  - 총량: 기존 균등 산포를 시뮬레이션해 개수를 추정한 뒤 그 25%만 배치
//  - 군락 배치: 나무/돌/식량 군락을 캠프 기준 서로 다른 방향(부채꼴 분할)에 생성
//  - 거리 비례 품질: 캠프에서 먼 군락일수록 크고 알차게.
//    나무 군락은 가까우면 잔가지 위주, 멀면 나무(통나무) 위주로 구성이 바뀜
//    ※ 카탈로그에 '통나무' 등 상위 자원 id가 없어 tree/stick 구성비와 밀도로 표현.
//      log 같은 id가 추가되면 PlaceOneCluster의 id 결정부에 끼우면 됨
//  - 벽 = 층 차이(절벽) 또는 배치물. 바위 지대는 "층 +2 돌출 지형(crag)"으로 생성
//  - 배치물이 놓인 칸은 통행 불가(규칙 4) → 배치물까지 고려한 최종 통행 검사 수행,
//    배치물이 길목을 막으면 자동으로 걷어냄(unpinch)
//  - 경사로는 낮은 쪽 칸에 표시, 완만 경사(Ramp Length)는 체인 단위 해석
//  - hearth 1개(시작) + hearth_site 1개(멀리, 도달 가능 보장)
//  - 고립 구역 없음: 도달 불가능한 열린 칸은 층을 올려 바위 덩어리로 봉인
public class MapGeneratorWindow : EditorWindow
{
    // ---- 파라미터 ----
    int seed = 12345;
    int width = 60;
    int depth = 60;

    [Range(1, 3)] int maxLevel = 2;
    float levelNoiseScale = 0.045f;   // 작을수록 고원이 큼직해짐
    float level1Threshold = 0.58f;
    float level2Threshold = 0.80f;
    int minPlateauSize = 12;          // 이보다 작은 고원은 평탄화
    int rampsPerPlateau = 2;
    [Range(1, 3)] int rampLength = 1; // 경사로 체인 길이. 1=45°, 2=27°, 3=18°

    float cragNoiseScale = 0.09f;
    float cragThreshold = 0.78f;      // 이 값 이상이면 바위 돌출 지형(층 +2)

    // 자원 금지 구역 (규칙 1)
    float hearthLightRadius = 4f;     // 모닥불 빛 반경(칸). 게임 쪽 값과 맞출 것
    float exclusionFactor = 1.75f;    // 금지 구역 R = 빛 반경 × 이 값 (1.5~2 권장)
    int innerSticksMin = 3;           // 금지 구역 안 잔가지 수
    int innerSticksMax = 5;

    // 총량 (규칙 2): 기존 균등 산포 추정치 × 비율
    float budgetRatio = 0.25f;
    // 기존 산포 밀도 파라미터 — 이제 총량 산정 기준으로만 쓰임
    float treeNoiseScale = 0.07f;
    float treeThreshold = 0.55f;
    float treeChance = 0.45f;
    float berryChance = 0.03f;
    float stoneChance = 0.02f;
    float stickChance = 0.02f;
    int minSpacing = 2;

    // 군락 (규칙 3, 4)
    int treeClusterCount = 3;
    int stoneClusterCount = 2;
    int berryClusterCount = 2;
    float clusterRadiusMin = 3f;      // 가까운(빈약한) 군락 반경
    float clusterRadiusMax = 6f;      // 먼(풍부한) 군락 반경

    int startClearRadius = 5;         // 시작 지점 지형 평탄화 반경

    string outputPath = "Assets/Maps/generated_map.json";
    Vector2 scrollPos;

    // ---- 내부 버퍼 ----
    int[,] levels;
    bool[,] ramps;
    bool[,] occupied;                 // placement 점유 = 통행 불가(규칙 4)
    bool[,] reachable;                // 배치물 미고려 기준 도달성
    List<MapPlacement> placements;
    System.Random rng;
    float noiseOx, noiseOz;

    static readonly int[] DX = { 1, -1, 0, 0 };
    static readonly int[] DZ = { 0, 0, 1, -1 };
    static readonly HashSet<string> ValidIds = new HashSet<string>
    { "hearth", "hearth_site", "tree", "stone", "stick", "berry", "crafting_station", "merchant_spot" };
    static readonly HashSet<string> RemovableIds = new HashSet<string>
    { "tree", "stone", "stick", "berry" };  // 길막 해소 시 걷어내도 되는 것들

    class ClusterSpec
    {
        public string type;           // "tree" / "stone" / "berry"
        public float dNorm;           // 0 = 가장 가까운 군락, 1 = 가장 먼 군락
        public int budget;            // 이 군락에 배치할 개수
        public float angleMin, angleMax; // 배정된 부채꼴(도)
    }

    [MenuItem("Tools/Map Generator")]
    static void Open() => GetWindow<MapGeneratorWindow>("Map Generator");

    void OnGUI()
    {
        // 버튼은 스크롤 밖 상단 고정 — 파라미터가 많아도 항상 보임
        if (GUILayout.Button("Generate", GUILayout.Height(32)))
            Generate();
        EditorGUILayout.Space();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.LabelField("기본", EditorStyles.boldLabel);
        seed = EditorGUILayout.IntField("Seed", seed);
        width = EditorGUILayout.IntField("Width", width);
        depth = EditorGUILayout.IntField("Depth", depth);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("층 (levels)", EditorStyles.boldLabel);
        maxLevel = EditorGUILayout.IntSlider("Max Level", maxLevel, 1, 3);
        levelNoiseScale = EditorGUILayout.Slider("Noise Scale", levelNoiseScale, 0.01f, 0.15f);
        level1Threshold = EditorGUILayout.Slider("Level 1 Threshold", level1Threshold, 0.3f, 0.9f);
        level2Threshold = EditorGUILayout.Slider("Level 2 Threshold", level2Threshold, 0.5f, 0.95f);
        minPlateauSize = EditorGUILayout.IntField("Min Plateau Size", minPlateauSize);
        rampsPerPlateau = EditorGUILayout.IntSlider("Ramps / Plateau", rampsPerPlateau, 1, 4);
        rampLength = EditorGUILayout.IntSlider("Ramp Length (완만함)", rampLength, 1, 3);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("바위 돌출 지형 (crag, 층 +2)", EditorStyles.boldLabel);
        cragNoiseScale = EditorGUILayout.Slider("Noise Scale", cragNoiseScale, 0.02f, 0.2f);
        cragThreshold = EditorGUILayout.Slider("Threshold", cragThreshold, 0.5f, 0.95f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("자원 금지 구역", EditorStyles.boldLabel);
        hearthLightRadius = EditorGUILayout.Slider("Hearth Light Radius", hearthLightRadius, 2f, 10f);
        exclusionFactor = EditorGUILayout.Slider("Exclusion Factor", exclusionFactor, 1.5f, 2f);
        EditorGUILayout.LabelField(" ", $"→ 금지 구역 반경 R = {hearthLightRadius * exclusionFactor:0.0}칸");
        innerSticksMin = EditorGUILayout.IntSlider("Inner Sticks Min", innerSticksMin, 1, 8);
        innerSticksMax = EditorGUILayout.IntSlider("Inner Sticks Max", innerSticksMax, innerSticksMin, 10);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("자원 총량", EditorStyles.boldLabel);
        budgetRatio = EditorGUILayout.Slider("Budget Ratio", budgetRatio, 0.05f, 1f);
        EditorGUILayout.HelpBox("기존 균등 산포로 뿌렸을 때의 추정 개수 × 이 비율이 총량. 아래 밀도 값들은 그 추정의 기준.", MessageType.None);
        treeNoiseScale = EditorGUILayout.Slider("Forest Noise Scale", treeNoiseScale, 0.02f, 0.2f);
        treeThreshold = EditorGUILayout.Slider("Forest Threshold", treeThreshold, 0.3f, 0.8f);
        treeChance = EditorGUILayout.Slider("Tree Chance", treeChance, 0.05f, 1f);
        berryChance = EditorGUILayout.Slider("Berry Chance", berryChance, 0f, 0.2f);
        stoneChance = EditorGUILayout.Slider("Stone Chance", stoneChance, 0f, 0.1f);
        stickChance = EditorGUILayout.Slider("Stick Chance", stickChance, 0f, 0.1f);
        minSpacing = EditorGUILayout.IntSlider("Min Spacing (산정용)", minSpacing, 1, 4);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("군락", EditorStyles.boldLabel);
        treeClusterCount = EditorGUILayout.IntSlider("Tree Clusters", treeClusterCount, 1, 5);
        stoneClusterCount = EditorGUILayout.IntSlider("Stone Clusters", stoneClusterCount, 1, 4);
        berryClusterCount = EditorGUILayout.IntSlider("Berry Clusters", berryClusterCount, 1, 4);
        clusterRadiusMin = EditorGUILayout.Slider("Cluster Radius Min", clusterRadiusMin, 2f, 6f);
        clusterRadiusMax = EditorGUILayout.Slider("Cluster Radius Max", clusterRadiusMax, clusterRadiusMin, 10f);

        EditorGUILayout.Space();
        startClearRadius = EditorGUILayout.IntSlider("Start Clear Radius", startClearRadius, 2, 10);
        outputPath = EditorGUILayout.TextField("Output Path", outputPath);

        EditorGUILayout.EndScrollView();
    }

    // =========================================================
    void Generate()
    {
        rng = new System.Random(seed);
        noiseOx = (float)(rng.NextDouble() * 10000.0);
        noiseOz = (float)(rng.NextDouble() * 10000.0);

        levels = new int[width, depth];
        ramps = new bool[width, depth];
        occupied = new bool[width, depth];
        placements = new List<MapPlacement>();

        GenerateLevels();
        RemoveSmallPlateaus();
        CarveRamps();
        GenerateCrags();

        Vector2Int start = FindStart();
        ClearAround(start, startClearRadius);
        SanitizeRamps();

        ComputeReachability(start);
        int sealedCells = SealUnreachable();
        SanitizeRamps();                                 // 봉인으로 무효화된 경사로 정리
        int[,] dist = ComputeReachability(start);        // 지형 확정 후 재계산

        AddPlacement("hearth", start.x, start.y, 0f);
        PlaceInnerSticks(start);                         // 규칙 1: 금지 구역 안 잔가지만
        PlaceResourceClusters(start);                    // 규칙 2~4: 군락 배치
        PlaceHearthSite(start, dist);

        int unpinched = UnpinchPlacements(start);        // 배치물이 막은 길목 해소

        bool ok = Validate(start);
        WriteJson();
        Debug.Log($"[MapGenerator] {(ok ? "완료" : "완료(경고 있음)")}: {outputPath} | placements {placements.Count}개 | 고립 봉인 {sealedCells}칸 | 길막 해소 {unpinched}개 | seed {seed}");
    }

    float ExclusionRadius => hearthLightRadius * exclusionFactor;

    // ---- 층 ----
    void GenerateLevels()
    {
        for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
            {
                float v = Noise(x, z, levelNoiseScale, 0f);
                int lv = 0;
                if (v >= level1Threshold) lv = 1;
                if (maxLevel >= 2 && v >= level2Threshold) lv = 2;
                levels[x, z] = Mathf.Min(lv, maxLevel);
            }
    }

    void RemoveSmallPlateaus()
    {
        bool changedAny = true;
        int guard = 0;
        while (changedAny && guard++ < 8)
        {
            changedAny = false;
            foreach (var region in FindRegions())
            {
                if (region.level == 0 || region.cells.Count >= minPlateauSize) continue;
                foreach (var c in region.cells)
                    levels[c.x, c.y] = region.level - 1;
                changedAny = true;
            }
        }
    }

    // ---- 경사로 (규격: 낮은 쪽 칸에 표시) ----
    void CarveRamps()
    {
        var regions = FindRegions();
        regions.Sort((a, b) => a.level.CompareTo(b.level));

        foreach (var region in regions)
        {
            if (region.level == 0) continue;
            int L = region.level;

            var candidates = new List<Vector2Int>();
            var seen = new HashSet<Vector2Int>();
            foreach (var c in region.cells)
                for (int d = 0; d < 4; d++)
                {
                    int lx = c.x + DX[d], lz = c.y + DZ[d];
                    if (!InBounds(lx, lz)) continue;
                    var low = new Vector2Int(lx, lz);
                    if (levels[lx, lz] != L - 1 || ramps[lx, lz] || !seen.Add(low)) continue;
                    if (IsGoodRampCell(low, out _)) candidates.Add(low);
                }

            if (candidates.Count == 0)
            {
                foreach (var c in region.cells)
                    levels[c.x, c.y] = L - 1;
                Debug.LogWarning($"[MapGenerator] 경사로 후보가 없는 고원(level {L}, {region.cells.Count}칸)을 평탄화했습니다.");
                continue;
            }

            Shuffle(candidates);
            int placed = 0;
            foreach (var c in candidates)
            {
                if (placed >= rampsPerPlateau) break;
                if (TooCloseToExistingRamp(c, region.cells.Count)) continue;
                PaintRampChain(c);
                placed++;
            }
            if (placed == 0) PaintRampChain(candidates[0]);
        }
    }

    bool IsGoodRampCell(Vector2Int c, out int upDir)
    {
        int lv = levels[c.x, c.y];
        int upCount = 0;
        upDir = -1;
        for (int d = 0; d < 4; d++)
        {
            int nx = c.x + DX[d], nz = c.y + DZ[d];
            if (InBounds(nx, nz) && levels[nx, nz] == lv + 1) { upCount++; upDir = d; }
        }
        if (upCount != 1) return false;
        int ox = c.x - DX[upDir], oz = c.y - DZ[upDir];
        return InBounds(ox, oz) && levels[ox, oz] == lv;
    }

    void PaintRampChain(Vector2Int head)
    {
        IsGoodRampCell(head, out int upDir);
        ramps[head.x, head.y] = true;
        int lv = levels[head.x, head.y];

        for (int i = 1; i < rampLength; i++)
        {
            int x = head.x - DX[upDir] * i, z = head.y - DZ[upDir] * i;
            if (!InBounds(x, z) || levels[x, z] != lv || ramps[x, z]) break;

            bool touchesUp = false;
            for (int d = 0; d < 4; d++)
            {
                int nx = x + DX[d], nz = z + DZ[d];
                if (InBounds(nx, nz) && levels[nx, nz] == lv + 1) { touchesUp = true; break; }
            }
            if (touchesUp) break;

            ramps[x, z] = true;
        }
    }

    void SanitizeRamps()
    {
        foreach (var group in FindRampGroups())
            if (!GroupHasUpNeighbor(group))
                foreach (var c in group)
                    ramps[c.x, c.y] = false;
    }

    List<List<Vector2Int>> FindRampGroups()
    {
        var groups = new List<List<Vector2Int>>();
        var visited = new bool[width, depth];
        var queue = new Queue<Vector2Int>();

        for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
            {
                if (!ramps[x, z] || visited[x, z]) continue;
                var group = new List<Vector2Int>();
                visited[x, z] = true;
                queue.Enqueue(new Vector2Int(x, z));
                while (queue.Count > 0)
                {
                    var c = queue.Dequeue();
                    group.Add(c);
                    for (int d = 0; d < 4; d++)
                    {
                        int nx = c.x + DX[d], nz = c.y + DZ[d];
                        if (!InBounds(nx, nz) || visited[nx, nz]) continue;
                        if (!ramps[nx, nz] || levels[nx, nz] != levels[c.x, c.y]) continue;
                        visited[nx, nz] = true;
                        queue.Enqueue(new Vector2Int(nx, nz));
                    }
                }
                groups.Add(group);
            }
        return groups;
    }

    bool GroupHasUpNeighbor(List<Vector2Int> group)
    {
        foreach (var c in group)
            for (int d = 0; d < 4; d++)
            {
                int nx = c.x + DX[d], nz = c.y + DZ[d];
                if (InBounds(nx, nz) && levels[nx, nz] == levels[c.x, c.y] + 1) return true;
            }
        return false;
    }

    bool TooCloseToExistingRamp(Vector2Int c, int regionSize)
    {
        int minDist = Mathf.Max(3, Mathf.RoundToInt(Mathf.Sqrt(regionSize) / 2f));
        for (int z = Mathf.Max(0, c.y - minDist); z <= Mathf.Min(depth - 1, c.y + minDist); z++)
            for (int x = Mathf.Max(0, c.x - minDist); x <= Mathf.Min(width - 1, c.x + minDist); x++)
                if (ramps[x, z]) return true;
        return false;
    }

    // ---- 바위 돌출 지형 ----
    void GenerateCrags()
    {
        for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
            {
                if (ramps[x, z] || HasRampNeighbor(x, z)) continue;
                float v = Noise(x, z, cragNoiseScale, 2000f);
                if (v >= cragThreshold)
                    levels[x, z] = Mathf.Min(9, levels[x, z] + 2);
            }
    }

    bool HasRampNeighbor(int x, int z)
    {
        for (int d = 0; d < 4; d++)
        {
            int nx = x + DX[d], nz = z + DZ[d];
            if (InBounds(nx, nz) && ramps[nx, nz]) return true;
        }
        return false;
    }

    // ---- 시작 지점 ----
    Vector2Int FindStart()
    {
        for (int z = 2; z < depth; z++)
            for (int off = 0; off < width / 2; off++)
                foreach (int x in new[] { width / 2 + off, width / 2 - off })
                {
                    if (!InBounds(x, z)) continue;
                    if (levels[x, z] == 0 && !ramps[x, z])
                        return new Vector2Int(x, z);
                }
        Debug.LogWarning("[MapGenerator] 시작 지점을 못 찾아 (0,0) 사용. 파라미터 확인 필요.");
        return new Vector2Int(0, 0);
    }

    void ClearAround(Vector2Int center, int radius)
    {
        for (int z = Mathf.Max(0, center.y - radius); z <= Mathf.Min(depth - 1, center.y + radius); z++)
            for (int x = Mathf.Max(0, center.x - radius); x <= Mathf.Min(width - 1, center.x + radius); x++)
            {
                if ((new Vector2Int(x, z) - center).sqrMagnitude > radius * radius) continue;
                levels[x, z] = 0;
                ramps[x, z] = false;
            }
    }

    // ---- 도달성 (지형 기준, 배치물 미고려) ----
    int[,] ComputeReachability(Vector2Int start)
    {
        reachable = new bool[width, depth];
        var dist = new int[width, depth];
        for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
                dist[x, z] = -1;

        var queue = new Queue<Vector2Int>();
        reachable[start.x, start.y] = true;
        dist[start.x, start.y] = 0;
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var c = queue.Dequeue();
            for (int d = 0; d < 4; d++)
            {
                int nx = c.x + DX[d], nz = c.y + DZ[d];
                if (!InBounds(nx, nz) || reachable[nx, nz]) continue;
                if (!Passable(c.x, c.y, nx, nz)) continue;
                reachable[nx, nz] = true;
                dist[nx, nz] = dist[c.x, c.y] + 1;
                queue.Enqueue(new Vector2Int(nx, nz));
            }
        }
        return dist;
    }

    bool Passable(int x1, int z1, int x2, int z2)
    {
        int diff = Mathf.Abs(levels[x2, z2] - levels[x1, z1]);
        return diff == 0 || (diff == 1 && (ramps[x1, z1] || ramps[x2, z2]));
    }

    int SealUnreachable()
    {
        int count = 0;
        for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
            {
                if (reachable[x, z]) continue;
                levels[x, z] = Mathf.Min(9, levels[x, z] + 2);
                ramps[x, z] = false;
                count++;
            }
        return count;
    }

    // ---- 규칙 1: 금지 구역 안 잔가지만 ----
    void PlaceInnerSticks(Vector2Int start)
    {
        int target = rng.Next(innerSticksMin, innerSticksMax + 1);
        float R = ExclusionRadius;
        int placed = 0, guard = 0;
        while (placed < target && guard++ < 500)
        {
            int x = start.x + rng.Next(-(int)R, (int)R + 1);
            int z = start.y + rng.Next(-(int)R, (int)R + 1);
            int sq = (new Vector2Int(x, z) - start).sqrMagnitude;
            if (sq < 2 * 2 || sq > R * R) continue;   // 화로 바로 옆은 피하고 R 안쪽만
            if (!CanPlaceAt(x, z)) continue;
            if (HasNeighborPlacement(x, z, 1)) continue;
            AddPlacement("stick", x, z, rng.Next(4) * 90f);
            placed++;
        }
        if (placed < innerSticksMin)
            Debug.LogWarning($"[MapGenerator] 금지 구역 안 잔가지 {target}개 중 {placed}개만 배치됨.");
    }

    // ---- 규칙 2~4: 군락 배치 ----
    void PlaceResourceClusters(Vector2Int start)
    {
        // 규칙 2: 기존 균등 산포를 드라이런으로 시뮬레이션해 총량 추정 → 25%
        int legacy = EstimateLegacyCount();
        int totalBudget = Mathf.Max(1, Mathf.RoundToInt(legacy * budgetRatio));
        Debug.Log($"[MapGenerator] 기존 산포 추정 {legacy}개 → 총량 {totalBudget}개 (×{budgetRatio:0.00})");

        // 유형별 예산 (기존 산포의 대략적 비율을 따름: 나무 위주)
        int treeBudget = Mathf.RoundToInt(totalBudget * 0.55f);
        int stoneBudget = Mathf.RoundToInt(totalBudget * 0.25f);
        int berryBudget = Mathf.Max(0, totalBudget - treeBudget - stoneBudget);

        var specs = new List<ClusterSpec>();
        AddClusterSpecs(specs, "tree", treeClusterCount, treeBudget);
        AddClusterSpecs(specs, "stone", stoneClusterCount, stoneBudget);
        AddClusterSpecs(specs, "berry", berryClusterCount, berryBudget);

        // 규칙 3: 부채꼴 분할로 방향 겹침 방지 — 군락 수만큼 360°를 나누고 무작위 배정
        Shuffle(specs);
        float rot = (float)(rng.NextDouble() * 360.0);
        float sector = 360f / specs.Count;
        for (int i = 0; i < specs.Count; i++)
        {
            specs[i].angleMin = rot + i * sector + sector * 0.15f; // 부채꼴 경계에 여유
            specs[i].angleMax = rot + (i + 1) * sector - sector * 0.15f;
        }

        // 거리 범위: 금지 구역 밖 ~ 도달 가능한 가장 먼 곳
        float minDist = ExclusionRadius + clusterRadiusMax + 1f;
        float maxDist = minDist + 5f;
        for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
                if (reachable[x, z])
                    maxDist = Mathf.Max(maxDist, Vector2.Distance(new Vector2(x, z), new Vector2(start.x, start.y)));

        foreach (var spec in specs)
            PlaceOneCluster(start, spec, minDist, maxDist);
    }

    // 규칙 4: 먼 군락일수록 예산(양)을 더 받음
    void AddClusterSpecs(List<ClusterSpec> specs, string type, int count, int typeBudget)
    {
        var weights = new float[count];
        float sum = 0f;
        for (int i = 0; i < count; i++)
        {
            float dNorm = count == 1 ? 0.6f : (float)i / (count - 1);
            weights[i] = 0.7f + 0.6f * dNorm;   // 먼 군락이 약 2배까지 풍부
            sum += weights[i];
        }
        for (int i = 0; i < count; i++)
        {
            float dNorm = count == 1 ? 0.6f : (float)i / (count - 1);
            specs.Add(new ClusterSpec
            {
                type = type,
                dNorm = dNorm,
                budget = Mathf.Max(1, Mathf.RoundToInt(typeBudget * weights[i] / sum))
            });
        }
    }

    void PlaceOneCluster(Vector2Int start, ClusterSpec spec, float minDist, float maxDist)
    {
        // 목표 거리: dNorm에 비례. 부채꼴 안에서 도달 가능한 중심 칸 탐색
        float target = Mathf.Lerp(minDist, maxDist, Mathf.Lerp(0.05f, 0.95f, spec.dNorm));
        Vector2Int center = default;
        bool found = false;

        for (int attempt = 0; attempt < 300 && !found; attempt++)
        {
            float ang = Mathf.Lerp(spec.angleMin, spec.angleMax, (float)rng.NextDouble()) * Mathf.Deg2Rad;
            float d = target * (0.85f + 0.3f * (float)rng.NextDouble());
            d = Mathf.Clamp(d, minDist, maxDist);
            int x = start.x + Mathf.RoundToInt(Mathf.Cos(ang) * d);
            int z = start.y + Mathf.RoundToInt(Mathf.Sin(ang) * d);
            if (!InBounds(x, z) || !reachable[x, z]) continue;
            center = new Vector2Int(x, z);
            found = true;
        }
        if (!found)
        {
            Debug.LogWarning($"[MapGenerator] {spec.type} 군락(dNorm {spec.dNorm:0.0}) 중심을 부채꼴 안에서 못 찾아 건너뜀. 시드/파라미터 조정 권장.");
            return;
        }

        // 규칙 4: 먼 군락일수록 반경도 큼
        float cr = Mathf.Lerp(clusterRadiusMin, clusterRadiusMax, spec.dNorm);
        // 나무 군락 구성비: 가까우면 잔가지 위주(0.7), 멀면 나무(통나무) 100%
        float stickRatio = spec.type == "tree" ? Mathf.Lerp(0.7f, 0f, spec.dNorm) : 0f;

        var cells = new List<Vector2Int>();
        int r = Mathf.CeilToInt(cr);
        for (int z = center.y - r; z <= center.y + r; z++)
            for (int x = center.x - r; x <= center.x + r; x++)
            {
                if (!InBounds(x, z)) continue;
                if ((new Vector2Int(x, z) - center).sqrMagnitude > cr * cr) continue;
                cells.Add(new Vector2Int(x, z));
            }
        Shuffle(cells);

        float R = ExclusionRadius;
        int placed = 0;
        foreach (var c in cells)
        {
            if (placed >= spec.budget) break;
            if ((c - start).sqrMagnitude <= R * R) continue;   // 금지 구역 침범 금지
            if (!CanPlaceAt(c.x, c.y)) continue;
            if (HasNeighborPlacement(c.x, c.y, 1)) continue;   // 군락 내 간격 1 (빽빽하되 길은 남김)

            string id = spec.type == "tree"
                ? (rng.NextDouble() < stickRatio ? "stick" : "tree")
                : spec.type;
            AddPlacement(id, c.x, c.y, rng.Next(4) * 90f);
            placed++;
        }
        if (placed < spec.budget / 2)
            Debug.LogWarning($"[MapGenerator] {spec.type} 군락({center.x},{center.y}): 예산 {spec.budget} 중 {placed}개만 배치됨 (지형 협소).");
    }

    // 기존 균등 산포를 그대로 드라이런해 개수를 추정 (실제 배치 없음, 별도 RNG)
    int EstimateLegacyCount()
    {
        var rng2 = new System.Random(seed * 397 + 13);
        var occ = new bool[width, depth];
        int count = 0;

        for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
            {
                if (!reachable[x, z] || ramps[x, z] || HasRampNeighbor(x, z)) continue;

                bool near = false;
                for (int zz = Mathf.Max(0, z - minSpacing); zz <= Mathf.Min(depth - 1, z + minSpacing) && !near; zz++)
                    for (int xx = Mathf.Max(0, x - minSpacing); xx <= Mathf.Min(width - 1, x + minSpacing) && !near; xx++)
                        if (occ[xx, zz]) near = true;
                if (near) continue;

                float forest = Noise(x, z, treeNoiseScale, 4000f);
                double roll = rng2.NextDouble();
                bool hit = forest >= treeThreshold
                    ? roll < treeChance + berryChance
                    : roll < stoneChance + stickChance;
                if (hit) { occ[x, z] = true; count++; }
            }
        return count;
    }

    // 배치물은 칸을 막으므로(규칙 4) 경사로와 그 옆칸은 피함
    bool CanPlaceAt(int x, int z)
    {
        return InBounds(x, z)
            && reachable[x, z]
            && !occupied[x, z]
            && !ramps[x, z]
            && !HasRampNeighbor(x, z);
    }

    // 두 번째 화로 터: 시작점에서 먼(보행 거리 기준) 도달 가능 칸. 고지 우대.
    void PlaceHearthSite(Vector2Int start, int[,] dist)
    {
        int maxDist = 0;
        for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
                maxDist = Mathf.Max(maxDist, dist[x, z]);

        Vector2Int best = default;
        int bestScore = int.MinValue;
        for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
            {
                if (!CanPlaceAt(x, z)) continue;
                if (dist[x, z] < maxDist * 7 / 10) continue;
                int score = dist[x, z] + levels[x, z] * width;
                if (score > bestScore) { bestScore = score; best = new Vector2Int(x, z); }
            }

        if (bestScore == int.MinValue)
        {
            Debug.LogWarning("[MapGenerator] hearth_site 후보를 못 찾았습니다. 임의 도달 가능 칸에 배치.");
            for (int z = 0; z < depth && bestScore == int.MinValue; z++)
                for (int x = 0; x < width && bestScore == int.MinValue; x++)
                    if (CanPlaceAt(x, z) && dist[x, z] > (int)ExclusionRadius)
                    { best = new Vector2Int(x, z); bestScore = 0; }
            if (bestScore == int.MinValue) { Debug.LogWarning("[MapGenerator] hearth_site 배치 실패."); return; }
        }
        AddPlacement("hearth_site", best.x, best.y, 0f);
    }

    bool HasNeighborPlacement(int cx, int cz, int r)
    {
        for (int z = Mathf.Max(0, cz - r); z <= Mathf.Min(depth - 1, cz + r); z++)
            for (int x = Mathf.Max(0, cx - r); x <= Mathf.Min(width - 1, cx + r); x++)
                if (occupied[x, z]) return true;
        return false;
    }

    void AddPlacement(string id, int x, int z, float rotY)
    {
        placements.Add(new MapPlacement { id = id, x = x, z = z, rotationY = rotY });
        occupied[x, z] = true;
    }

    // ---- 배치물 길막 해소 ----
    int UnpinchPlacements(Vector2Int start)
    {
        int removed = 0;
        for (int guard = 0; guard < 100; guard++)
        {
            var vis = BfsWithPlacements(start);

            var isolated = new List<Vector2Int>();
            for (int z = 0; z < depth; z++)
                for (int x = 0; x < width; x++)
                    if (!occupied[x, z] && reachable[x, z] && !vis[x, z])
                        isolated.Add(new Vector2Int(x, z));
            if (isolated.Count == 0) return removed;

            MapPlacement culprit = null;
            foreach (var p in placements)
            {
                if (!RemovableIds.Contains(p.id)) continue;
                bool touchVis = false, touchIso = false;
                for (int d = 0; d < 4; d++)
                {
                    int nx = p.x + DX[d], nz = p.z + DZ[d];
                    if (!InBounds(nx, nz) || occupied[nx, nz]) continue;
                    if (!Passable(p.x, p.z, nx, nz)) continue;
                    if (vis[nx, nz]) touchVis = true;
                    else if (reachable[nx, nz]) touchIso = true;
                }
                if (touchVis && touchIso) { culprit = p; break; }
            }

            if (culprit == null)
            {
                Debug.LogWarning($"[MapGenerator] 배치물 고립 구역 {isolated.Count}칸을 해소하지 못했습니다. 시드를 바꿔보세요.");
                return removed;
            }
            placements.Remove(culprit);
            occupied[culprit.x, culprit.z] = false;
            removed++;
        }
        return removed;
    }

    bool[,] BfsWithPlacements(Vector2Int start)
    {
        var vis = new bool[width, depth];
        var queue = new Queue<Vector2Int>();
        vis[start.x, start.y] = true;
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var c = queue.Dequeue();
            for (int d = 0; d < 4; d++)
            {
                int nx = c.x + DX[d], nz = c.y + DZ[d];
                if (!InBounds(nx, nz) || vis[nx, nz] || occupied[nx, nz]) continue;
                if (!Passable(c.x, c.y, nx, nz)) continue;
                vis[nx, nz] = true;
                queue.Enqueue(new Vector2Int(nx, nz));
            }
        }
        return vis;
    }

    // ---- 검증 ----
    bool Validate(Vector2Int start)
    {
        bool ok = true;
        void Fail(string msg) { ok = false; Debug.LogWarning("[MapGenerator/검증] " + msg); }

        // 경사로: 체인 단위 — 묶음 중 한 칸 이상이 한 단 높은 칸에 닿아야 함
        foreach (var group in FindRampGroups())
        {
            if (!GroupHasUpNeighbor(group))
                Fail($"무효 경사로 체인 ({group[0].x},{group[0].y}) 외 {group.Count - 1}칸: 한 단 높은 이웃 없음");
            if (rampLength == 1 && group.Count > 1)
                Fail($"경사로 체인 ({group[0].x},{group[0].y}): Ramp Length 1인데 {group.Count}칸이 붙어 있음");
        }

        // placements: id 유효, 범위 내, 칸 중복 없음
        var counts = new Dictionary<string, int>();
        var cells = new HashSet<(int, int)>();
        foreach (var p in placements)
        {
            if (!ValidIds.Contains(p.id)) Fail($"알 수 없는 id '{p.id}' ({p.x},{p.z})");
            if (!InBounds(p.x, p.z)) { Fail($"범위 밖 placement ({p.x},{p.z})"); continue; }
            if (!cells.Add((p.x, p.z))) Fail($"같은 칸에 배치물 중복 ({p.x},{p.z})");
            counts[p.id] = counts.GetValueOrDefault(p.id) + 1;
        }

        if (counts.GetValueOrDefault("hearth") != 1) Fail("hearth가 정확히 1개가 아님");
        if (counts.GetValueOrDefault("hearth_site") != 1) Fail("hearth_site가 정확히 1개가 아님");

        // 규칙 1: 금지 구역 검사 — 안쪽엔 잔가지만, 수량 3~5
        float R = ExclusionRadius;
        int innerSticks = 0;
        foreach (var p in placements)
        {
            if (!Near(p, start, Mathf.FloorToInt(R))) continue;
            if (p.id == "stick") innerSticks++;
            else if (p.id == "tree" || p.id == "stone" || p.id == "berry")
                Fail($"금지 구역(R={R:0.0}) 안에 자원 '{p.id}' ({p.x},{p.z})");
        }
        if (innerSticks < innerSticksMin || innerSticks > innerSticksMax)
            Fail($"금지 구역 안 잔가지 {innerSticks}개 (목표 {innerSticksMin}~{innerSticksMax})");

        // 돌 접근성: 규격의 "시작 주변 돌" 제약 ↔ 금지 구역 규칙이 상충하므로
        // "가장 가까운 돌이 지나치게 멀지 않은지"로 완화해서 검사
        var stones = placements.Where(p => p.id == "stone").ToList();
        if (stones.Count == 0) Fail("맵에 돌이 하나도 없음 (도끼 제작 불가)");
        else
        {
            float nearest = stones.Min(p => Vector2.Distance(new Vector2(p.x, p.z), new Vector2(start.x, start.y)));
            if (nearest > R * 3f)
                Fail($"가장 가까운 돌이 {nearest:0.0}칸 거리 (권장 {R * 3f:0.0} 이내). 돌 군락 배치 확인 필요");
        }

        // 두 화로 사이 실제 보행 경로 (배치물 차단 포함)
        var vis = BfsWithPlacements(start);
        var site = placements.FirstOrDefault(p => p.id == "hearth_site");
        if (site != null && InBounds(site.x, site.z))
        {
            bool reached = vis[site.x, site.z];
            for (int d = 0; d < 4 && !reached; d++)
            {
                int nx = site.x + DX[d], nz = site.z + DZ[d];
                if (InBounds(nx, nz) && vis[nx, nz] && Passable(site.x, site.z, nx, nz)) reached = true;
            }
            if (!reached) Fail("hearth → hearth_site 보행 경로 없음 (배치물 차단 포함)");
        }

        // 고립 구역 (배치물 차단 포함 기준)
        int isolatedCount = 0;
        for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
                if (!occupied[x, z] && reachable[x, z] && !vis[x, z]) isolatedCount++;
        if (isolatedCount > 0) Fail($"고립 구역 {isolatedCount}칸 잔존");

        if (ok) Debug.Log("[MapGenerator/검증] 체크리스트 전 항목 통과");
        return ok;
    }

    bool Near(MapPlacement p, Vector2Int c, int r)
        => (new Vector2Int(p.x, p.z) - c).sqrMagnitude <= r * r;

    // ---- 저장 ----
    void WriteJson()
    {
        var data = new MapData
        {
            cellSize = 1f,
            width = width,
            depth = depth,
            levels = RowsFrom((x, z) => (char)('0' + levels[x, z])),
            ramps = RowsFrom((x, z) => ramps[x, z] ? '/' : '.'),
            placements = placements.ToArray()
        };

        string json = JsonUtility.ToJson(data, true);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        File.WriteAllText(outputPath, json);
        AssetDatabase.Refresh();
    }

    string[] RowsFrom(Func<int, int, char> f)
    {
        var rows = new string[depth];
        for (int z = 0; z < depth; z++)
        {
            var row = new char[width];
            for (int x = 0; x < width; x++)
                row[x] = f(x, z);
            rows[z] = new string(row);
        }
        return rows;
    }

    // ---- 유틸 ----
    struct Region { public int level; public List<Vector2Int> cells; }

    List<Region> FindRegions()
    {
        var result = new List<Region>();
        var visited = new bool[width, depth];
        var queue = new Queue<Vector2Int>();

        for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
            {
                if (visited[x, z]) continue;
                int lv = levels[x, z];
                var cells = new List<Vector2Int>();
                visited[x, z] = true;
                queue.Enqueue(new Vector2Int(x, z));
                while (queue.Count > 0)
                {
                    var c = queue.Dequeue();
                    cells.Add(c);
                    for (int d = 0; d < 4; d++)
                    {
                        int nx = c.x + DX[d], nz = c.y + DZ[d];
                        if (!InBounds(nx, nz) || visited[nx, nz] || levels[nx, nz] != lv) continue;
                        visited[nx, nz] = true;
                        queue.Enqueue(new Vector2Int(nx, nz));
                    }
                }
                result.Add(new Region { level = lv, cells = cells });
            }
        return result;
    }

    float Noise(int x, int z, float scale, float channelOffset)
    {
        return Mathf.PerlinNoise(
            (x + noiseOx + channelOffset) * scale,
            (z + noiseOz + channelOffset) * scale);
    }

    bool InBounds(int x, int z) => x >= 0 && x < width && z >= 0 && z < depth;

    void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}