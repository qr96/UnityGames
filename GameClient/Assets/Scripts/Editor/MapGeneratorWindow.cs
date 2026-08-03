using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// 맵 생성기 (에디터 전용). Assets/Editor/ 폴더에 넣을 것.
// 메뉴: Tools > Map Generator
//
// "혹한 정착지 — 맵 파일 규격" 준수:
//  - 경사로는 낮은 쪽 칸에 표시, 한 단 높은 이웃이 정확히 한 방향(대각선 없음)
//  - 경사 완급: 같은 층 경사로를 한 줄로 이어 칠하면 완만해짐 (Ramp Length 1=45°, 2=27°, 3=18°)
//    ※ 규격 체크리스트("모든 / 칸에 +1 이웃")와 완만 경사로 규칙이 상충함 — 여기선
//      "체인 중 한 칸 이상이 +1 칸에 닿으면 유효"로 해석. 규격 확정 전까지 기본값 1 권장.
//  - hearth 1개(시작) + hearth_site 1개(멀리, 도달 가능 보장)
//  - 시작 지점 주변에 stick/stone 배치 (도끼 제작용)
//  - 고립 구역 없음: 시작점에서 도달 불가능한 열린 칸은 blocked로 봉인
//  - 생성 후 규격의 검증 체크리스트를 코드로 자체 검사, 콘솔에 결과 출력
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
    [Range(1, 3)] int rampLength = 1;  // 경사로 체인 길이. 1=45°, 2=27°, 3=18°

    float rockNoiseScale = 0.09f;
    float rockThreshold = 0.74f;

    float treeNoiseScale = 0.07f;
    float treeThreshold = 0.55f;      // 숲 판정
    float treeChance = 0.45f;
    float berryChance = 0.03f;        // 숲 칸에서 나무 대신 열매
    float stoneChance = 0.02f;
    float stickChance = 0.02f;
    int minSpacing = 2;               // placement 간 최소 간격(칸)

    int startClearRadius = 5;
    int starterSticks = 3;            // 시작 지점 근처 보장 수량
    int starterStones = 2;

    string outputPath = "Assets/Maps/generated_map.json";

    // ---- 내부 버퍼 ----
    int[,] levels;
    bool[,] blocked;
    bool[,] ramps;
    bool[,] occupied;
    bool[,] reachable;
    List<MapPlacement> placements;
    System.Random rng;
    float noiseOx, noiseOz;

    static readonly int[] DX = { 1, -1, 0, 0 };
    static readonly int[] DZ = { 0, 0, 1, -1 };
    static readonly HashSet<string> ValidIds = new HashSet<string>
    { "hearth", "hearth_site", "tree", "stone", "stick", "berry", "crafting_station", "merchant_spot" };

    [MenuItem("Tools/Map Generator")]
    static void Open() => GetWindow<MapGeneratorWindow>("Map Generator");

    void OnGUI()
    {
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
        EditorGUILayout.LabelField("바위벽 (blocked)", EditorStyles.boldLabel);
        rockNoiseScale = EditorGUILayout.Slider("Noise Scale", rockNoiseScale, 0.02f, 0.2f);
        rockThreshold = EditorGUILayout.Slider("Threshold", rockThreshold, 0.5f, 0.95f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("산포 (placements)", EditorStyles.boldLabel);
        treeNoiseScale = EditorGUILayout.Slider("Forest Noise Scale", treeNoiseScale, 0.02f, 0.2f);
        treeThreshold = EditorGUILayout.Slider("Forest Threshold", treeThreshold, 0.3f, 0.8f);
        treeChance = EditorGUILayout.Slider("Tree Chance", treeChance, 0.05f, 1f);
        berryChance = EditorGUILayout.Slider("Berry Chance", berryChance, 0f, 0.2f);
        stoneChance = EditorGUILayout.Slider("Stone Chance", stoneChance, 0f, 0.1f);
        stickChance = EditorGUILayout.Slider("Stick Chance", stickChance, 0f, 0.1f);
        minSpacing = EditorGUILayout.IntSlider("Min Spacing", minSpacing, 1, 4);

        EditorGUILayout.Space();
        startClearRadius = EditorGUILayout.IntSlider("Start Clear Radius", startClearRadius, 2, 10);
        starterSticks = EditorGUILayout.IntSlider("Starter Sticks", starterSticks, 1, 6);
        starterStones = EditorGUILayout.IntSlider("Starter Stones", starterStones, 1, 6);
        outputPath = EditorGUILayout.TextField("Output Path", outputPath);

        EditorGUILayout.Space();
        if (GUILayout.Button("Generate", GUILayout.Height(32)))
            Generate();
    }

    // =========================================================
    void Generate()
    {
        rng = new System.Random(seed);
        noiseOx = (float)(rng.NextDouble() * 10000.0);
        noiseOz = (float)(rng.NextDouble() * 10000.0);

        levels = new int[width, depth];
        blocked = new bool[width, depth];
        ramps = new bool[width, depth];
        occupied = new bool[width, depth];
        placements = new List<MapPlacement>();

        GenerateLevels();
        RemoveSmallPlateaus();
        CarveRamps();
        GenerateRocks();

        Vector2Int start = FindStart();
        ClearAround(start, startClearRadius);
        SanitizeRamps(); // 클리어로 지형이 바뀌며 무효가 된 경사로 제거

        int[,] dist = ComputeReachability(start);
        int sealedCells = SealUnreachable();

        AddPlacement("hearth", start.x, start.y, 0f);
        PlaceStarterResources(start);
        ScatterVegetation(start);
        PlaceHearthSite(start, dist);

        bool ok = Validate(start);
        WriteJson();
        Debug.Log($"[MapGenerator] {(ok ? "완료" : "완료(경고 있음)")}: {outputPath} | placements {placements.Count}개 | 고립 봉인 {sealedCells}칸 | seed {seed}");
    }

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
        // 낮은 층 고원부터 처리해야 2층까지 "한 단씩 오르는 사다리"가 자연히 이어짐
        var regions = FindRegions();
        regions.Sort((a, b) => a.level.CompareTo(b.level));

        foreach (var region in regions)
        {
            if (region.level == 0) continue;
            int L = region.level;

            // 후보 = 이 고원과 인접한 한 층 아래(L-1) 칸
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
                // 경사로를 놓을 자리가 없는 고립 고원 → 한 층 낮춰서 포기
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

    // 규격의 유효 경사로 + 생성기 자체 기준:
    //  - 한 단 높은 이웃이 정확히 1방향 (경사 방향 유일, 대각선 없음)
    //  - 그 반대쪽 이웃은 같은 층 (진입로가 걸어서 이어짐)
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
        return InBounds(ox, oz) && levels[ox, oz] == lv && !blocked[c.x, c.y];
    }

    // 완만 경사: 머리 칸(+1 칸에 닿는 칸)에서 경사 반대 방향으로 rampLength만큼 이어 칠함
    void PaintRampChain(Vector2Int head)
    {
        IsGoodRampCell(head, out int upDir); // 후보 검증을 통과한 칸이므로 방향 보장
        ramps[head.x, head.y] = true;
        int lv = levels[head.x, head.y];

        for (int i = 1; i < rampLength; i++)
        {
            int x = head.x - DX[upDir] * i, z = head.y - DZ[upDir] * i;
            if (!InBounds(x, z) || levels[x, z] != lv || blocked[x, z] || ramps[x, z]) break;

            // 꼬리 칸이 다른 고원에 닿으면 의도치 않은 경사면이 생기므로 중단
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

    // 무효 경사로 제거. 체인 단위 해석:
    // 같은 층으로 이어진 경사로 묶음 중 어느 칸도 +1 칸에 닿지 않으면 묶음 전체 제거
    void SanitizeRamps()
    {
        // blocked 위 경사로부터 제거
        for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
                if (ramps[x, z] && blocked[x, z]) ramps[x, z] = false;

        foreach (var group in FindRampGroups())
            if (!GroupHasUpNeighbor(group))
                foreach (var c in group)
                    ramps[c.x, c.y] = false;
    }

    // 같은 층으로 인접한 경사로 칸 묶음(체인)들을 찾음
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

    // ---- 바위벽 ----
    void GenerateRocks()
    {
        for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
            {
                if (ramps[x, z]) continue;
                float v = Noise(x, z, rockNoiseScale, 2000f);
                if (v >= rockThreshold)
                    blocked[x, z] = true;
            }
        // 경사로 상하좌우는 뚫어줌 (통로가 벽에 막히는 것 방지)
        for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
            {
                if (!ramps[x, z]) continue;
                for (int d = 0; d < 4; d++)
                {
                    int nx = x + DX[d], nz = z + DZ[d];
                    if (InBounds(nx, nz)) blocked[nx, nz] = false;
                }
            }
    }

    // ---- 시작 지점 ----
    Vector2Int FindStart()
    {
        for (int z = 2; z < depth; z++)
            for (int off = 0; off < width / 2; off++)
                foreach (int x in new[] { width / 2 + off, width / 2 - off })
                {
                    if (!InBounds(x, z)) continue;
                    if (levels[x, z] == 0 && !blocked[x, z] && !ramps[x, z])
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
                blocked[x, z] = false;
                levels[x, z] = 0;
                ramps[x, z] = false;
            }
    }

    // ---- 도달성 ----
    // 규격의 통행 규칙 그대로 BFS. 거리 배열 반환(-1 = 도달 불가)
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
                if (!InBounds(nx, nz) || reachable[nx, nz] || blocked[nx, nz]) continue;

                int diff = Mathf.Abs(levels[nx, nz] - levels[c.x, c.y]);
                bool passable = diff == 0
                    || (diff == 1 && (ramps[c.x, c.y] || ramps[nx, nz]));
                if (!passable) continue;

                reachable[nx, nz] = true;
                dist[nx, nz] = dist[c.x, c.y] + 1;
                queue.Enqueue(new Vector2Int(nx, nz));
            }
        }
        return dist;
    }

    // 고립 구역 금지: 도달 불가능한 열린 칸을 전부 blocked로 봉인
    int SealUnreachable()
    {
        int count = 0;
        for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
            {
                if (blocked[x, z] || reachable[x, z]) continue;
                blocked[x, z] = true;
                ramps[x, z] = false;
                count++;
            }
        return count;
    }

    // ---- 배치 ----
    // 시작 지점 근처 stick/stone 보장 (도끼 제작용)
    void PlaceStarterResources(Vector2Int start)
    {
        PlaceNear(start, "stick", starterSticks, 2, startClearRadius);
        PlaceNear(start, "stone", starterStones, 2, startClearRadius);
    }

    void PlaceNear(Vector2Int center, string id, int count, int rMin, int rMax)
    {
        int placed = 0, guard = 0;
        while (placed < count && guard++ < 500)
        {
            int x = center.x + rng.Next(-rMax, rMax + 1);
            int z = center.y + rng.Next(-rMax, rMax + 1);
            int sq = (new Vector2Int(x, z) - center).sqrMagnitude;
            if (!InBounds(x, z) || sq < rMin * rMin || sq > rMax * rMax) continue;
            if (blocked[x, z] || ramps[x, z] || occupied[x, z] || !reachable[x, z]) continue;
            AddPlacement(id, x, z, rng.Next(4) * 90f);
            placed++;
        }
        if (placed < count)
            Debug.LogWarning($"[MapGenerator] 시작 지점 근처 {id} {count}개 중 {placed}개만 배치됨.");
    }

    void ScatterVegetation(Vector2Int start)
    {
        for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
            {
                if (blocked[x, z] || ramps[x, z] || occupied[x, z] || !reachable[x, z]) continue;
                if ((new Vector2Int(x, z) - start).sqrMagnitude <= startClearRadius * startClearRadius) continue;
                if (HasNeighborPlacement(x, z, minSpacing)) continue;

                float forest = Noise(x, z, treeNoiseScale, 4000f);
                double roll = rng.NextDouble();
                if (forest >= treeThreshold)
                {
                    if (roll < treeChance) AddPlacement("tree", x, z, rng.Next(4) * 90f);
                    else if (roll < treeChance + berryChance) AddPlacement("berry", x, z, 0f);
                }
                else
                {
                    if (roll < stoneChance) AddPlacement("stone", x, z, rng.Next(4) * 90f);
                    else if (roll < stoneChance + stickChance) AddPlacement("stick", x, z, rng.Next(4) * 90f);
                }
            }
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
                if (!reachable[x, z] || blocked[x, z] || ramps[x, z] || occupied[x, z]) continue;
                if (dist[x, z] < maxDist * 7 / 10) continue;              // 충분히 먼 곳만
                int score = dist[x, z] + levels[x, z] * width;            // 고지 크게 우대
                if (score > bestScore) { bestScore = score; best = new Vector2Int(x, z); }
            }

        if (bestScore == int.MinValue)
        {
            Debug.LogWarning("[MapGenerator] hearth_site 후보를 못 찾았습니다. 시작점 근처에 강제 배치.");
            PlaceNear(start, "hearth_site", 1, startClearRadius, startClearRadius * 2);
            return;
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

    // ---- 검증 (규격의 체크리스트) ----
    bool Validate(Vector2Int start)
    {
        bool ok = true;
        void Fail(string msg) { ok = false; Debug.LogWarning("[MapGenerator/검증] " + msg); }

        // 경사로: 체인 단위로 검사 — 묶음 중 한 칸 이상이 한 단 높은 칸에 닿아야 함
        // (완만 경사로의 꼬리 칸은 +1 이웃이 없는 게 정상. 규격 체크리스트의 칸 단위
        //  문구와 상충하므로 규격 담당과 해석 확정 필요)
        foreach (var group in FindRampGroups())
        {
            if (!GroupHasUpNeighbor(group))
                Fail($"무효 경사로 체인 ({group[0].x},{group[0].y}) 외 {group.Count - 1}칸: 한 단 높은 이웃 없음");
            if (rampLength == 1 && group.Count > 1)
                Fail($"경사로 체인 ({group[0].x},{group[0].y}): Ramp Length 1인데 {group.Count}칸이 붙어 있음 (서로 다른 경사로가 인접)");
        }

        // placements: id 유효, 범위 내, blocked 아님
        var counts = new Dictionary<string, int>();
        foreach (var p in placements)
        {
            if (!ValidIds.Contains(p.id)) Fail($"알 수 없는 id '{p.id}' ({p.x},{p.z})");
            if (!InBounds(p.x, p.z)) { Fail($"범위 밖 placement ({p.x},{p.z})"); continue; }
            if (blocked[p.x, p.z]) Fail($"blocked 칸 위 placement '{p.id}' ({p.x},{p.z})");
            counts[p.id] = counts.GetValueOrDefault(p.id) + 1;
        }

        // hearth / hearth_site 각 1개
        if (counts.GetValueOrDefault("hearth") != 1) Fail("hearth가 정확히 1개가 아님");
        if (counts.GetValueOrDefault("hearth_site") != 1) Fail("hearth_site가 정확히 1개가 아님");

        // 두 화로 사이 경로 (hearth_site가 reachable 위에만 놓이므로 사실상 보장, 재확인)
        var site = placements.FirstOrDefault(p => p.id == "hearth_site");
        if (site != null && (!InBounds(site.x, site.z) || !reachable[site.x, site.z]))
            Fail("hearth → hearth_site 경로 없음");

        // 시작 자원
        bool anyStickNear = placements.Any(p => p.id == "stick" && Near(p, start, startClearRadius + 1));
        bool anyStoneNear = placements.Any(p => p.id == "stone" && Near(p, start, startClearRadius + 1));
        if (!anyStickNear) Fail("시작 지점 근처에 stick 없음");
        if (!anyStoneNear) Fail("시작 지점 근처에 stone 없음");

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
            blocked = RowsFrom((x, z) => blocked[x, z] ? '#' : '.'),
            ramps = RowsFrom((x, z) => ramps[x, z] ? '/' : '.'),
            placements = placements.ToArray()
        };

        string json = JsonUtility.ToJson(data, true);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        File.WriteAllText(outputPath, json);
        AssetDatabase.Refresh();
    }

    // rows[0] = z=0 규격 준수. 파일에서 첫 줄이 맵 남쪽(아래)임에 주의.
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