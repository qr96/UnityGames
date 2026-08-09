using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// 맵 생성기 (에디터 전용). Assets/Editor/ 폴더에 넣을 것.
// 메뉴: Tools > Map Generator
//
// 지형 규칙:
//  1) 이산 높이 레벨 0~2. 인접 층 차이 최대 1 — 위반 지형은 EnforceTerracing()이 계단식 분해
//  2) 경사로 고정 규격(길이 N타일). 오목 코너 인접 후보 우선, 일자 구간은 차선
//  3) 자원 고지대는 경사로 2개 보장, 미달 고원은 경사로 회수 후 배경(고립 허용) 처리
//  4) flood fill 검증·디버그 로그 유지
//
// 자원 배치:
//  - 금지 구역(모닥불 빛 반경 × 배율) 안엔 잔가지만. 잔가지는 지정 밴드(기본 4~7타일)에 배치
//  - 총량은 직접 목표 방식: 나무 35~45(가까운 군락 10±2), 베리 10~14, 돌 12+
//    (구 budgetRatio 방식은 추정치 의존이 커서 범위 보장이 불가능해 교체함)
//  - 최근접 돌 거리 상한(기본 15타일) 미충족 시 보정 배치 1회 시도
//  - 군락은 부채꼴 분할로 방향 분리, 먼 군락일수록 예산·반경 큼
//  - 경사로 체인 양옆 절벽 칸에 장식(decorations, 비차단) 좌표 출력
public class MapGeneratorWindow : EditorWindow
{
    // ---- 파라미터 ----
    int seed = 12345;
    int width = 60;
    int depth = 60;

    [Range(1, 2)] int maxLevel = 2;
    float levelNoiseScale = 0.045f;
    float level1Threshold = 0.63f;
    float level2Threshold = 0.82f;
    int minPlateauSize = 12;
    [Range(2, 4)] int rampsPerPlateau = 2;
    [Range(1, 3)] int rampLength = 1;   // 고정 규격 N타일. 1=45° (규격 체크리스트를 문자 그대로 만족)

    // 경사로 스타일: Protruding = 절벽 밖(낮은 땅)에 부착 / Inset = 절벽 안쪽을 파서 새김(노치)
    enum RampStyle { Protruding, Inset }
    RampStyle rampStyle = RampStyle.Protruding;
    [Range(1, 3)] int rampWidth = 2;    // 경사로 최대 폭(칸). 경사로마다 1~이 값 무작위, 지형 협소 시 자동 축소

    float cragNoiseScale = 0.09f;
    float cragThreshold = 0.78f;

    // 경사로 드레싱 (렌더링 장식용, 게임플레이 무영향)
    bool emitDecorations = true;
    string decorationId = "rubble";

    // 자원 금지 구역
    float hearthLightRadius = 4f;
    float exclusionFactor = 1.75f;

    // 초기 잔가지: 개수 4~6, 배치 밴드 4~7타일 (화로 상호작용 반경과 겹침 방지)
    int innerSticksMin = 4;
    int innerSticksMax = 6;
    float innerBandMin = 4f;
    float innerBandMax = 7f;

    // 목표 총량 (직접 지정)
    int treeTotalMin = 35;
    int treeTotalMax = 45;
    int nearTreeBase = 10;            // 가까운 나무 군락의 나무 수 = base ± jitter
    int nearTreeJitter = 2;
    int berryTotalMin = 10;
    int berryTotalMax = 14;
    int stoneTotalMin = 12;
    int stoneTotalMax = 16;
    float stoneNearestMaxDist = 15f;  // 최근접 돌 거리 상한(타일)

    // 군락
    int treeClusterCount = 3;
    int stoneClusterCount = 2;
    int berryClusterCount = 2;
    float clusterRadiusMin = 3f;
    float clusterRadiusMax = 6f;

    int startClearRadius = 5;

    // 산포 모드: UniformLegacy = 예전 균등 산포(노이즈+확률, 맵 전체), Clusters = 군락+금지구역+목표총량
    enum ScatterMode { UniformLegacy, Clusters }
    ScatterMode scatterMode = ScatterMode.UniformLegacy;

    // 예전 균등 산포 파라미터 (UniformLegacy 모드에서만 사용)
    float treeNoiseScale = 0.07f;
    float treeThreshold = 0.55f;      // 숲 판정
    float treeChance = 0.45f;
    float berryChance = 0.03f;
    float stoneChance = 0.02f;
    float stickChance = 0.02f;
    int minSpacing = 2;
    int starterSticks = 3;            // 시작 지점 근처 보장 수량
    int starterStones = 2;

    string outputPath = "Assets/Maps/generated_map.json";
    Vector2 scrollPos;

    // ---- 내부 버퍼 ----
    int[,] levels;
    bool[,] ramps;
    bool[,] isCrag;
    bool[,] occupied;
    bool[,] reachable;                // 시작점에서 갈 수 있는 칸 (전진, 낙하 포함)
    bool[,] canReturn;                // 시작점으로 돌아올 수 있는 칸 (역방향)
    bool[,] roundTrip;                // 왕복 가능 = reachable ∧ canReturn
    bool[,] resourceAllowed;
    List<MapPlacement> placements;
    List<MapPlacement> decorations;
    System.Random rng;
    float noiseOx, noiseOz;
    int nearTreePlaced;               // 가까운 나무 군락에 실제 심긴 나무 수 (검증용)
    Vector2Int nearTreeCenter;        // 가까운 나무 군락 중심 (보충 배치용)
    bool hasNearTreeCenter;

    static readonly int[] DX = { 1, -1, 0, 0 };
    static readonly int[] DZ = { 0, 0, 1, -1 };
    static readonly int[] DX8 = { 1, -1, 0, 0, 1, 1, -1, -1 };
    static readonly int[] DZ8 = { 0, 0, 1, -1, 1, -1, 1, -1 };
    static readonly HashSet<string> ValidIds = new HashSet<string>
    { "hearth", "hearth_site", "tree", "stone", "stick", "berry", "crafting_station", "merchant_spot" };
    static readonly HashSet<string> RemovableIds = new HashSet<string>
    { "tree", "stone", "stick", "berry" };

    class ClusterSpec
    {
        public string type;
        public float dNorm;
        public int budget;
        public bool isNearTree;                       // 가까운 나무 군락 (10±2 검증 대상)
        public float maxCenterDist = float.MaxValue;  // 최근접 돌 군락용 중심 거리 상한
        public float angleMin, angleMax;
    }

    [MenuItem("Tools/Map Generator")]
    static void Open() => GetWindow<MapGeneratorWindow>("Map Generator");

    void OnGUI()
    {
        if (GUILayout.Button("Generate", GUILayout.Height(32)))
            Generate();
        EditorGUILayout.Space();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.LabelField("기본", EditorStyles.boldLabel);
        seed = EditorGUILayout.IntField("Seed", seed);
        width = EditorGUILayout.IntField("Width", width);
        depth = EditorGUILayout.IntField("Depth", depth);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("층 (levels, 0~2 계단식)", EditorStyles.boldLabel);
        maxLevel = EditorGUILayout.IntSlider("Max Level", maxLevel, 1, 2);
        levelNoiseScale = EditorGUILayout.Slider("Noise Scale", levelNoiseScale, 0.01f, 0.15f);
        level1Threshold = EditorGUILayout.Slider("Level 1 Threshold", level1Threshold, 0.3f, 0.9f);
        level2Threshold = EditorGUILayout.Slider("Level 2 Threshold", level2Threshold, 0.5f, 0.95f);
        minPlateauSize = EditorGUILayout.IntField("Min Plateau Size", minPlateauSize);
        rampsPerPlateau = EditorGUILayout.IntSlider("Ramps / Plateau (최소 2)", rampsPerPlateau, 2, 4);
        rampLength = EditorGUILayout.IntSlider("Ramp Length (고정 N타일)", rampLength, 1, 3);
        rampStyle = (RampStyle)EditorGUILayout.EnumPopup("Ramp Style", rampStyle);
        rampWidth = EditorGUILayout.IntSlider("Ramp Width (최대 폭)", rampWidth, 1, 3);
        if (rampStyle == RampStyle.Inset)
            EditorGUILayout.HelpBox("절벽 안쪽을 파서 새기는 노치형. 경사로 칸의 +1 이웃이 여러 개(양옆 벽)가 되므로 로더의 경사 방향 추론이 이를 지원하는지 확인 필요.", MessageType.Warning);
        emitDecorations = EditorGUILayout.Toggle("Ramp Decorations", emitDecorations);
        if (emitDecorations)
            decorationId = EditorGUILayout.TextField("Decoration Id", decorationId);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("배경 돌출 지형 (crag, 층 +1·경사로 없음)", EditorStyles.boldLabel);
        cragNoiseScale = EditorGUILayout.Slider("Noise Scale", cragNoiseScale, 0.02f, 0.2f);
        cragThreshold = EditorGUILayout.Slider("Threshold", cragThreshold, 0.5f, 0.95f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("자원 산포 방식", EditorStyles.boldLabel);
        scatterMode = (ScatterMode)EditorGUILayout.EnumPopup("Scatter Mode", scatterMode);
        if (scatterMode == ScatterMode.UniformLegacy)
        {
            EditorGUILayout.HelpBox("예전 방식: 노이즈+확률로 맵 전체에 균등 산포. 캠프 주변에도 자원이 깔림.", MessageType.None);
            treeNoiseScale = EditorGUILayout.Slider("Forest Noise Scale", treeNoiseScale, 0.02f, 0.2f);
            treeThreshold = EditorGUILayout.Slider("Forest Threshold", treeThreshold, 0.3f, 0.8f);
            treeChance = EditorGUILayout.Slider("Tree Chance", treeChance, 0.05f, 1f);
            berryChance = EditorGUILayout.Slider("Berry Chance", berryChance, 0f, 0.2f);
            stoneChance = EditorGUILayout.Slider("Stone Chance", stoneChance, 0f, 0.1f);
            stickChance = EditorGUILayout.Slider("Stick Chance", stickChance, 0f, 0.1f);
            minSpacing = EditorGUILayout.IntSlider("Min Spacing", minSpacing, 1, 4);
            starterSticks = EditorGUILayout.IntSlider("Starter Sticks", starterSticks, 1, 6);
            starterStones = EditorGUILayout.IntSlider("Starter Stones", starterStones, 1, 6);
        }
        else
        {
            EditorGUILayout.HelpBox("군락 방식: 금지 구역 + 방향별 군락 + 목표 총량 (기획 스펙).", MessageType.None);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("자원 금지 구역 / 초기 잔가지", EditorStyles.boldLabel);
            hearthLightRadius = EditorGUILayout.Slider("Hearth Light Radius", hearthLightRadius, 2f, 10f);
            exclusionFactor = EditorGUILayout.Slider("Exclusion Factor", exclusionFactor, 1.5f, 2f);
            EditorGUILayout.LabelField(" ", $"→ 금지 구역 반경 R = {hearthLightRadius * exclusionFactor:0.0}칸");
            innerSticksMin = EditorGUILayout.IntSlider("Inner Sticks Min", innerSticksMin, 1, 8);
            innerSticksMax = EditorGUILayout.IntSlider("Inner Sticks Max", innerSticksMax, innerSticksMin, 10);
            innerBandMin = EditorGUILayout.Slider("Stick Band Min", innerBandMin, 2f, innerBandMax);
            innerBandMax = EditorGUILayout.Slider("Stick Band Max", innerBandMax, innerBandMin, 12f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("목표 총량 (직접 지정)", EditorStyles.boldLabel);
            treeTotalMin = EditorGUILayout.IntSlider("Trees Min", treeTotalMin, 5, treeTotalMax);
            treeTotalMax = EditorGUILayout.IntSlider("Trees Max", treeTotalMax, treeTotalMin, 100);
            nearTreeBase = EditorGUILayout.IntSlider("Near Cluster Trees", nearTreeBase, 3, 20);
            nearTreeJitter = EditorGUILayout.IntSlider("Near Cluster ±", nearTreeJitter, 0, 5);
            berryTotalMin = EditorGUILayout.IntSlider("Berries Min", berryTotalMin, 0, berryTotalMax);
            berryTotalMax = EditorGUILayout.IntSlider("Berries Max", berryTotalMax, berryTotalMin, 40);
            stoneTotalMin = EditorGUILayout.IntSlider("Stones Min", stoneTotalMin, 1, stoneTotalMax);
            stoneTotalMax = EditorGUILayout.IntSlider("Stones Max", stoneTotalMax, stoneTotalMin, 40);
            stoneNearestMaxDist = EditorGUILayout.Slider("Stone Nearest Max Dist", stoneNearestMaxDist, 8f, 30f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("군락", EditorStyles.boldLabel);
            treeClusterCount = EditorGUILayout.IntSlider("Tree Clusters", treeClusterCount, 2, 5);
            stoneClusterCount = EditorGUILayout.IntSlider("Stone Clusters", stoneClusterCount, 1, 4);
            berryClusterCount = EditorGUILayout.IntSlider("Berry Clusters", berryClusterCount, 1, 4);
            clusterRadiusMin = EditorGUILayout.Slider("Cluster Radius Min", clusterRadiusMin, 2f, 6f);
            clusterRadiusMax = EditorGUILayout.Slider("Cluster Radius Max", clusterRadiusMax, clusterRadiusMin, 10f);
        }

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
        isCrag = new bool[width, depth];
        occupied = new bool[width, depth];
        placements = new List<MapPlacement>();
        decorations = new List<MapPlacement>();
        nearTreePlaced = 0;
        hasNearTreeCenter = false;

        // --- 지형 ---
        GenerateLevels();
        EnforceTerracing();
        RemoveSmallPlateaus();
        EnforceTerracing();
        GenerateCrags();
        EnforceTerracing();

        Vector2Int start = FindStart();
        ClearAround(start, startClearRadius);
        EnforceTerracing();

        // --- 경사로 ---
        int backgroundPlateaus = CarveRamps();
        ComputeReachability(start);
        int decorativeRamps = RemoveUnreachableRamps();
        ComputeReachability(start);
        int escapeRamps = CarveEscapeRamps(start);       // 원웨이 포켓 보정 (내부에서 반복 재검사)
        int[,] dist = ComputeReachability(start);
        ComputeReturnability(start);
        BuildRoundTrip();
        if (escapeRamps > 0)
            Debug.Log($"[MapGenerator] 탈출 경사로 {escapeRamps}개 추가됨");

        if (emitDecorations) BuildRampDecorations();

        BuildResourceMask(out int eligiblePlateaus, out int maskedPlateaus);
        LogTerrainStats(backgroundPlateaus, decorativeRamps, eligiblePlateaus, maskedPlateaus);

        // --- 배치 ---
        AddPlacement("hearth", start.x, start.y, 0f);
        if (scatterMode == ScatterMode.Clusters)
        {
            PlaceInnerSticks(start);
            PlaceResourceClusters(start);
            EnsureNearbyStone(start);
        }
        else
        {
            PlaceStarterResourcesLegacy(start);
            ScatterUniformLegacy(start);
        }
        PlaceHearthSite(start, dist);

        int unpinched = UnpinchPlacements(start);

        // unpinch가 잔가지를 걷어냈으면 보충 후 한 번 더 길막 검사
        int minSticks = scatterMode == ScatterMode.Clusters ? innerSticksMin : starterSticks;
        int stickRadius = scatterMode == ScatterMode.Clusters ? Mathf.CeilToInt(innerBandMax) : startClearRadius;
        int sticksNow = placements.Count(p => p.id == "stick");
        if (sticksNow < minSticks)
        {
            TopUpAround(start, "stick", minSticks - sticksNow, start, stickRadius);
            unpinched += UnpinchPlacements(start);
        }

        bool ok = Validate(start);
        WriteJson();
        Debug.Log($"[MapGenerator] {(ok ? "완료" : "완료(경고 있음)")}: {outputPath} | placements {placements.Count}개 | 길막 해소 {unpinched}개 | seed {seed}");
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

    void EnforceTerracing()
    {
        bool changed = true;
        int guard = 0;
        while (changed && guard++ < 16)
        {
            changed = false;
            for (int z = 0; z < depth; z++)
                for (int x = 0; x < width; x++)
                    for (int d = 0; d < 4; d++)
                    {
                        int nx = x + DX[d], nz = z + DZ[d];
                        if (!InBounds(nx, nz)) continue;
                        if (levels[x, z] > levels[nx, nz] + 1)
                        {
                            levels[x, z] = levels[nx, nz] + 1;
                            changed = true;
                        }
                    }
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

    void GenerateCrags()
    {
        for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
            {
                float v = Noise(x, z, cragNoiseScale, 2000f);
                if (v < cragThreshold) continue;
                levels[x, z] = Mathf.Min(maxLevel, levels[x, z] + 1);
                isCrag[x, z] = true;
            }
    }

    // ---- 시작 지점 ----
    // 맵 중앙에서 가장 가까운 0층·비crag 칸 (규격: hearth = "중앙 화로")
    Vector2Int FindStart()
    {
        var center = new Vector2Int(width / 2, depth / 2);
        for (int radius = 0; radius <= Mathf.Max(width, depth); radius++)
        {
            // 반지름을 넓혀가며 링 단위 탐색 → 중앙에서 가장 가까운 유효 칸
            for (int z = center.y - radius; z <= center.y + radius; z++)
                for (int x = center.x - radius; x <= center.x + radius; x++)
                {
                    // 링 테두리만 검사 (내부는 이전 반지름에서 이미 확인)
                    if (Mathf.Max(Mathf.Abs(x - center.x), Mathf.Abs(z - center.y)) != radius) continue;
                    if (!InBounds(x, z)) continue;
                    if (levels[x, z] == 0 && !isCrag[x, z])
                        return new Vector2Int(x, z);
                }
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
                isCrag[x, z] = false;
            }
    }

    // ---- 경사로 ----
    int CarveRamps()
    {
        int background = 0;
        var regions = FindRegions();
        regions.Sort((a, b) => a.level.CompareTo(b.level));

        foreach (var region in regions)
        {
            if (region.level == 0) continue;

            int cragCells = region.cells.Count(c => isCrag[c.x, c.y]);
            if (cragCells * 2 > region.cells.Count) { background++; continue; }

            // 후보 수집 — 스타일별
            var candidates = new List<(Vector2Int cell, int dir)>();
            if (rampStyle == RampStyle.Protruding)
            {
                // 오목 코너 인접 후보 우선, 일자 구간 후보는 차선
                var corner = new List<(Vector2Int cell, int dir)>();
                var straight = new List<(Vector2Int cell, int dir)>();
                var seen = new HashSet<Vector2Int>();
                foreach (var c in region.cells)
                    for (int d = 0; d < 4; d++)
                    {
                        int lx = c.x + DX[d], lz = c.y + DZ[d];
                        if (!InBounds(lx, lz)) continue;
                        var low = new Vector2Int(lx, lz);
                        if (levels[lx, lz] != region.level - 1 || ramps[lx, lz] || !seen.Add(low)) continue;
                        if (IsGoodRampCell(low, out int upDir) && CanPaintFullChain(low, upDir))
                            (IsCornerAdjacent(low, upDir) ? corner : straight).Add((low, upDir));
                    }
                Shuffle(corner);
                Shuffle(straight);
                candidates.AddRange(corner);
                candidates.AddRange(straight);
            }
            else
            {
                // Inset: 고원 가장자리 칸(바깥쪽 한 단 아래 이웃이 정확히 1방향) 중
                // 안쪽으로 N타일을 안전하게 파낼 수 있는 곳. 노치는 일자 절벽에서도
                // 자연스러워서 코너 우선 분류는 적용하지 않음
                foreach (var c in region.cells)
                {
                    int downCount = 0, outDir = -1;
                    for (int d = 0; d < 4; d++)
                    {
                        int nx = c.x + DX[d], nz = c.y + DZ[d];
                        if (InBounds(nx, nz) && levels[nx, nz] == region.level - 1) { downCount++; outDir = d; }
                    }
                    if (downCount != 1) continue;
                    if (CanCarveInset(c, outDir)) candidates.Add((c, outDir));
                }
                Shuffle(candidates);
            }

            var painted = new List<(Vector2Int cell, int prevLevel)>();
            int chains = 0;
            foreach (var (cell, dir) in candidates)
            {
                if (chains >= rampsPerPlateau) break;
                if (TooCloseToExistingRamp(cell, region.cells.Count)) continue;
                int targetW = rng.Next(1, rampWidth + 1);   // 경사로마다 폭 1~rampWidth 무작위
                if (rampStyle == RampStyle.Protruding)
                {
                    if (ramps[cell.x, cell.y] || !CanPaintFullChain(cell, dir)) continue;
                    foreach (var col in CollectWideColumns(cell, dir, targetW, protruding: true))
                        PaintRampChain(col, dir, painted);
                }
                else
                {
                    if (ramps[cell.x, cell.y] || !CanCarveInset(cell, dir)) continue; // 앞선 트렌치로 무효화됐을 수 있어 재확인
                    foreach (var col in CollectWideColumns(cell, dir, targetW, protruding: false))
                        PaintInsetTrench(col, dir, painted);
                }
                chains++;
            }

            if (chains < 2)
            {
                foreach (var (c, prev) in painted)
                {
                    ramps[c.x, c.y] = false;
                    levels[c.x, c.y] = prev;   // Inset은 파낸 층도 원복
                }
                background++;
                Debug.Log($"[MapGenerator] 고원(level {region.level}, {region.cells.Count}칸): 고정 길이 {rampLength} 경사로를 2개 확보하지 못해 배경 처리 (확보 {chains}개).");
            }
        }
        return background;
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

    // 오목 코너 인접 판정:
    //  (a) 후보 칸 8방향의 +1 칸이 '정면 줄'(upDir 쪽 3칸) 밖에도 있으면 절벽 라인에 꺾임 포함
    //  (b) 오목 코너 칸(같은 층인데 상하좌우 +1 이웃 2개 이상)에 8방향으로 인접
    bool IsCornerAdjacent(Vector2Int c, int upDir)
    {
        int lv = levels[c.x, c.y];

        int ux = DX[upDir], uz = DZ[upDir];
        int rx = uz, rz = ux;
        var frontRow = new HashSet<(int, int)>
        {
            (c.x + ux, c.y + uz),
            (c.x + ux + rx, c.y + uz + rz),
            (c.x + ux - rx, c.y + uz - rz)
        };
        for (int d = 0; d < 8; d++)
        {
            int nx = c.x + DX8[d], nz = c.y + DZ8[d];
            if (!InBounds(nx, nz) || levels[nx, nz] != lv + 1) continue;
            if (!frontRow.Contains((nx, nz))) return true;
        }

        for (int d = 0; d < 8; d++)
        {
            int nx = c.x + DX8[d], nz = c.y + DZ8[d];
            if (!InBounds(nx, nz) || levels[nx, nz] != lv) continue;
            if (CardinalUpCount(nx, nz) >= 2) return true;
        }
        return false;
    }

    int CardinalUpCount(int x, int z)
    {
        int lv = levels[x, z], count = 0;
        for (int d = 0; d < 4; d++)
        {
            int nx = x + DX[d], nz = z + DZ[d];
            if (InBounds(nx, nz) && levels[nx, nz] == lv + 1) count++;
        }
        return count;
    }

    bool CanPaintFullChain(Vector2Int head, int upDir)
    {
        int lv = levels[head.x, head.y];
        for (int i = 1; i < rampLength; i++)
        {
            int x = head.x - DX[upDir] * i, z = head.y - DZ[upDir] * i;
            if (!InBounds(x, z) || levels[x, z] != lv || ramps[x, z]) return false;
            for (int d = 0; d < 4; d++)
            {
                int nx = x + DX[d], nz = z + DZ[d];
                if (InBounds(nx, nz) && levels[nx, nz] == lv + 1) return false;
            }
        }
        return true;
    }

    void PaintRampChain(Vector2Int head, int upDir, List<(Vector2Int cell, int prevLevel)> painted)
    {
        for (int i = 0; i < rampLength; i++)
        {
            int x = head.x - DX[upDir] * i, z = head.y - DZ[upDir] * i;
            painted.Add((new Vector2Int(x, z), levels[x, z]));
            ramps[x, z] = true;
        }
    }

    // Inset(노치) 파기 가능 판정. edge = 고원 가장자리 칸, outDir = 바깥(한 단 아래) 방향.
    // 트렌치는 edge에서 안쪽으로 N칸:
    //  - 전부 같은 층 L, 미경사로
    //  - 양옆이 같은 층 L (파낸 뒤 옆벽이 생기고, 폭 1짜리 능선을 잘라 고원을 쪼개지 않음)
    //  - 어떤 칸도 L+1 이웃이 없어야 함 (파낸 뒤 2단 절벽 금지 규칙 위반 방지)
    //  - 트렌치 끝 너머가 같은 층 L (머리 칸이 한 단 위와 접해 유효 경사로가 됨)
    bool CanCarveInset(Vector2Int edge, int outDir)
    {
        int L = levels[edge.x, edge.y];
        int ix = -DX[outDir], iz = -DZ[outDir];   // 안쪽 방향
        int px = DZ[outDir], pz = DX[outDir];     // 수직 방향

        for (int i = 0; i < rampLength; i++)
        {
            int x = edge.x + ix * i, z = edge.y + iz * i;
            if (!InBounds(x, z) || levels[x, z] != L || ramps[x, z]) return false;

            int ax = x + px, az = z + pz;
            int bx = x - px, bz = z - pz;
            if (!InBounds(ax, az) || levels[ax, az] != L) return false;
            if (!InBounds(bx, bz) || levels[bx, bz] != L) return false;

            for (int d = 0; d < 4; d++)
            {
                int nx = x + DX[d], nz = z + DZ[d];
                if (InBounds(nx, nz) && levels[nx, nz] >= L + 1) return false;
            }
        }
        int ex = edge.x + ix * rampLength, ez = edge.y + iz * rampLength;
        return InBounds(ex, ez) && levels[ex, ez] == L;
    }

    // 트렌치 파기: 층을 1 낮추고 경사로 표시 (painted에 원래 층 기록 → 롤백 가능)
    void PaintInsetTrench(Vector2Int edge, int outDir, List<(Vector2Int cell, int prevLevel)> painted)
    {
        int L = levels[edge.x, edge.y];
        int ix = -DX[outDir], iz = -DZ[outDir];
        for (int i = 0; i < rampLength; i++)
        {
            int x = edge.x + ix * i, z = edge.y + iz * i;
            painted.Add((new Vector2Int(x, z), L));
            levels[x, z] = L - 1;
            ramps[x, z] = true;
        }
    }

    // 폭 넓은 경사로: 기준 열(primary) 좌우로 유효한 열을 붙여 폭을 넓힘.
    // 열들은 반드시 연속이어야 하고(±1 다음에야 ±2), 지형이 안 되면 그만큼 좁아짐.
    // 판정은 전부 칠하기 전(원본 지형)에 수행 — Inset은 열끼리 서로의 옆벽 조건을 공유하므로 순서 중요
    List<Vector2Int> CollectWideColumns(Vector2Int primary, int dir, int targetW, bool protruding)
    {
        var cols = new List<Vector2Int> { primary };
        if (targetW <= 1) return cols;

        int px = DZ[dir], pz = DX[dir];   // dir에 수직인 방향
        var have = new HashSet<int> { 0 };
        foreach (int o in new[] { 1, -1, 2, -2 })
        {
            if (cols.Count >= targetW) break;
            if (!have.Contains(o > 0 ? o - 1 : o + 1)) continue;   // 연속성

            var c2 = new Vector2Int(primary.x + px * o, primary.y + pz * o);
            if (!InBounds(c2.x, c2.y) || ramps[c2.x, c2.y]) continue;

            bool ok;
            if (protruding)
            {
                ok = IsGoodRampCell(c2, out int d2) && d2 == dir && CanPaintFullChain(c2, dir);
            }
            else
            {
                // Inset 열: 같은 층이고, 입구(바깥) 방향이 한 단 아래로 열려 있어야 함
                int ex = c2.x + DX[dir], ez = c2.y + DZ[dir];
                ok = levels[c2.x, c2.y] == levels[primary.x, primary.y]
                    && InBounds(ex, ez)
                    && levels[ex, ez] == levels[c2.x, c2.y] - 1
                    && CanCarveInset(c2, dir);
            }
            if (!ok) continue;
            cols.Add(c2);
            have.Add(o);
        }
        return cols;
    }

    // 경사로 체인 주변(대각 어깨·노치 옆벽 포함)의 절벽(+1) 칸에 장식 좌표 생성.
    // 통로(출구) 판정: 체인 칸 c의 정방향 d에 +1 칸이 있고, 반대편(c-d)이 체인과 같은 층이면
    // 플레이어가 실제로 걸어 오르는 방향이므로 장식 제외. (노치의 옆벽은 반대편도 +1이라 장식 대상)
    void BuildRampDecorations()
    {
        decorations.Clear();
        var seen = new HashSet<Vector2Int>();

        foreach (var group in FindRampGroups())
        {
            foreach (var c in group)
                for (int d = 0; d < 8; d++)
                {
                    int nx = c.x + DX8[d], nz = c.y + DZ8[d];
                    var n = new Vector2Int(nx, nz);
                    if (!InBounds(nx, nz) || levels[nx, nz] != levels[c.x, c.y] + 1) continue;

                    // 정방향(상하좌우) +1이고 반대편이 체인 층이면 = 오르는 통로 → 제외
                    if (d < 4)
                    {
                        int ox = c.x - DX8[d], oz = c.y - DZ8[d];
                        if (InBounds(ox, oz) && levels[ox, oz] == levels[c.x, c.y]) continue;
                    }

                    if (!seen.Add(n)) continue;
                    decorations.Add(new MapPlacement { id = decorationId, x = nx, z = nz, rotationY = rng.Next(4) * 90f });
                }
        }
        Debug.Log($"[MapGenerator] 경사로 드레싱 '{decorationId}' {decorations.Count}개 생성 (decorations 필드, 비차단 장식)");
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

    int RemoveUnreachableRamps()
    {
        int removed = 0;
        for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
            {
                if (ramps[x, z] && !reachable[x, z])
                {
                    ramps[x, z] = false;
                    removed++;
                }
            }
        return removed;
    }

    // 방향성 통행 규칙: (x1,z1) → (x2,z2)
    //  같은 층 = 통과 / 한 단 아래 = 뛰어내리기 자유 / 한 단 위 = 경사로 필요 / 그 외 불가
    bool PassableFrom(int x1, int z1, int x2, int z2)
    {
        int diff = levels[x2, z2] - levels[x1, z1];
        if (diff == 0) return true;
        if (diff == -1) return true;
        if (diff == 1) return ramps[x1, z1] || ramps[x2, z2];
        return false;
    }

    // 연결성 검사 1: 시작점에서 갈 수 있는가 (전진 BFS)
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
                if (!PassableFrom(c.x, c.y, nx, nz)) continue;
                reachable[nx, nz] = true;
                dist[nx, nz] = dist[c.x, c.y] + 1;
                queue.Enqueue(new Vector2Int(nx, nz));
            }
        }
        return dist;
    }

    // 연결성 검사 2: 시작점으로 돌아올 수 있는가 (간선을 뒤집은 역방향 BFS)
    void ComputeReturnability(Vector2Int start)
    {
        canReturn = new bool[width, depth];
        var queue = new Queue<Vector2Int>();
        canReturn[start.x, start.y] = true;
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var c = queue.Dequeue();
            for (int d = 0; d < 4; d++)
            {
                int nx = c.x + DX[d], nz = c.y + DZ[d];
                if (!InBounds(nx, nz) || canReturn[nx, nz]) continue;
                if (!PassableFrom(nx, nz, c.x, c.y)) continue;   // n에서 c로 올 수 있으면 n도 귀환 가능
                canReturn[nx, nz] = true;
                queue.Enqueue(new Vector2Int(nx, nz));
            }
        }
    }

    void BuildRoundTrip()
    {
        roundTrip = new bool[width, depth];
        for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
                roundTrip[x, z] = reachable[x, z] && canReturn[x, z];
    }

    // 원웨이 포켓(뛰어내리면 못 돌아오는 구역) 탈출 경사로 자동 보정.
    // 포켓 칸 중 유효한 경사로 자리(+1 방향 유일, 목표 칸이 귀환 가능 지역)에 폭 1 경사로를 파고 재검사
    int CarveEscapeRamps(Vector2Int start)
    {
        int added = 0;
        for (int iter = 0; iter < 24; iter++)
        {
            ComputeReturnability(start);
            var oneWay = new List<Vector2Int>();
            for (int z = 0; z < depth; z++)
                for (int x = 0; x < width; x++)
                    if (reachable[x, z] && !canReturn[x, z])
                        oneWay.Add(new Vector2Int(x, z));
            if (oneWay.Count == 0) break;

            Shuffle(oneWay);
            bool carved = false;
            foreach (var c in oneWay)
            {
                if (ramps[c.x, c.y]) continue;
                if (!IsGoodRampCell(c, out int upDir) || !CanPaintFullChain(c, upDir)) continue;
                int tx = c.x + DX[upDir], tz = c.y + DZ[upDir];
                if (!canReturn[tx, tz]) continue;   // 오른 곳에서 집에 갈 수 있어야 탈출로가 됨
                var dummy = new List<(Vector2Int, int)>();
                PaintRampChain(c, upDir, dummy);
                ComputeReachability(start);          // 새 경사로로 전진 도달성도 갱신
                added++;
                carved = true;
                Debug.Log($"[MapGenerator] 원웨이 포켓 탈출 경사로 추가 ({c.x},{c.y})");
                break;
            }
            if (!carved)
            {
                // 경사로 자리가 없는 구덩이(폭 1칸 등)는 층을 올려 메움 →
                // 둘러싼 링과 같은 층의 평지가 되어 걸어서 오갈 수 있음
                foreach (var c in oneWay)
                {
                    levels[c.x, c.y] = Mathf.Min(maxLevel, levels[c.x, c.y] + 1);
                    ramps[c.x, c.y] = false;
                }
                ComputeReachability(start);
                Debug.Log($"[MapGenerator] 탈출 경사로 자리가 없는 원웨이 구덩이 {oneWay.Count}칸을 메움 (층 +1)");
            }
        }
        return added;
    }

    // ---- 자원 마스크 ----
    void BuildResourceMask(out int eligiblePlateaus, out int maskedPlateaus)
    {
        resourceAllowed = new bool[width, depth];
        eligiblePlateaus = 0;
        maskedPlateaus = 0;

        var regions = FindRegions();
        var regionId = new int[width, depth];
        for (int i = 0; i < regions.Count; i++)
            foreach (var c in regions[i].cells)
                regionId[c.x, c.y] = i;

        var chainCount = new int[regions.Count];
        foreach (var group in FindRampGroups())
        {
            var targets = new HashSet<int>();
            foreach (var c in group)
                for (int d = 0; d < 4; d++)
                {
                    int nx = c.x + DX[d], nz = c.y + DZ[d];
                    if (InBounds(nx, nz) && levels[nx, nz] == levels[c.x, c.y] + 1)
                        targets.Add(regionId[nx, nz]);
                }
            foreach (var t in targets)
                chainCount[t]++;
        }

        for (int i = 0; i < regions.Count; i++)
        {
            if (regions[i].level == 0) continue;
            bool anyRoundTrip = regions[i].cells.Any(c => roundTrip[c.x, c.y]);
            if (anyRoundTrip && chainCount[i] >= 2) eligiblePlateaus++;
            else maskedPlateaus++;
        }

        for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
            {
                if (!roundTrip[x, z]) continue;   // 자원은 왕복 가능 지역에만 (원웨이 구덩이 미끼 방지)
                int rid = regionId[x, z];
                resourceAllowed[x, z] = regions[rid].level == 0 || chainCount[rid] >= 2;
            }
    }

    void LogTerrainStats(int backgroundPlateaus, int decorativeRamps, int eligiblePlateaus, int maskedPlateaus)
    {
        int unreachableOpen = 0, oneWay = 0;
        for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
            {
                if (!reachable[x, z]) unreachableOpen++;
                else if (!canReturn[x, z]) oneWay++;
            }

        Debug.Log($"[MapGenerator/지형] 배경 고원 {backgroundPlateaus}개, 장식 경사로 회수 {decorativeRamps}칸, " +
                  $"자원 허용 고원 {eligiblePlateaus}개 / 자원 금지 고원 {maskedPlateaus}개, " +
                  $"도달 불가(배경) {unreachableOpen}칸, 원웨이(가면 못 돌아옴) {oneWay}칸");
    }

    // ---- 배치: 예전 균등 산포 (UniformLegacy 모드) ----
    // 시작 지점 근처 stick/stone 보장 (도끼 제작용)
    void PlaceStarterResourcesLegacy(Vector2Int start)
    {
        PlaceNearLegacy(start, "stick", starterSticks, 2, startClearRadius);
        PlaceNearLegacy(start, "stone", starterStones, 2, startClearRadius);
    }

    void PlaceNearLegacy(Vector2Int center, string id, int count, int rMin, int rMax)
    {
        int placed = 0, guard = 0;
        while (placed < count && guard++ < 500)
        {
            int x = center.x + rng.Next(-rMax, rMax + 1);
            int z = center.y + rng.Next(-rMax, rMax + 1);
            int sq = (new Vector2Int(x, z) - center).sqrMagnitude;
            if (sq < rMin * rMin || sq > rMax * rMax) continue;
            if (!CanPlaceResource(x, z)) continue;
            if (HasNeighborPlacement(x, z, 1)) continue;
            AddPlacement(id, x, z, rng.Next(4) * 90f);
            placed++;
        }
        if (placed < count)
            Debug.LogWarning($"[MapGenerator] 시작 지점 근처 {id} {count}개 중 {placed}개만 배치됨.");
    }

    // 노이즈+확률 균등 산포: 숲 노이즈가 높으면 나무/베리, 낮으면 돌/잔가지
    void ScatterUniformLegacy(Vector2Int start)
    {
        for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
            {
                if (!CanPlaceResource(x, z)) continue;
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

    // ---- 배치: 초기 잔가지 (밴드 4~7타일) ----
    void PlaceInnerSticks(Vector2Int start)
    {
        int target = rng.Next(innerSticksMin, innerSticksMax + 1);
        int placed = 0, guard = 0;
        int bandMaxI = Mathf.CeilToInt(innerBandMax);
        while (placed < target && guard++ < 500)
        {
            int x = start.x + rng.Next(-bandMaxI, bandMaxI + 1);
            int z = start.y + rng.Next(-bandMaxI, bandMaxI + 1);
            float d = Vector2.Distance(new Vector2(x, z), new Vector2(start.x, start.y));
            if (d < innerBandMin || d > innerBandMax) continue;
            if (!CanPlaceResource(x, z)) continue;
            if (HasNeighborPlacement(x, z, 1)) continue;
            AddPlacement("stick", x, z, rng.Next(4) * 90f);
            placed++;
        }
        if (placed < innerSticksMin)
            Debug.LogWarning($"[MapGenerator] 초기 잔가지 {target}개 중 {placed}개만 배치됨. Stick Band 폭 또는 Start Clear Radius 확인.");
    }

    // ---- 배치: 군락 (직접 목표 총량) ----
    void PlaceResourceClusters(Vector2Int start)
    {
        int treeTotal = rng.Next(treeTotalMin, treeTotalMax + 1);
        int berryTotal = rng.Next(berryTotalMin, berryTotalMax + 1);
        int stoneTotal = rng.Next(stoneTotalMin, stoneTotalMax + 1);

        // 가까운 나무 군락: base ± jitter, 나머지 군락 몫도 남겨둠
        int nearTrees = nearTreeBase + rng.Next(-nearTreeJitter, nearTreeJitter + 1);
        nearTrees = Mathf.Clamp(nearTrees, 1, treeTotal - (treeClusterCount - 1));

        var specs = new List<ClusterSpec>();

        // 나무: 군락 0 = 가까운 군락(고정 예산), 나머지는 거리 가중 분배
        var farTreeBudgets = SplitBudgetByDistance(treeTotal - nearTrees, treeClusterCount - 1);
        for (int i = 0; i < treeClusterCount; i++)
        {
            float dNorm = treeClusterCount == 1 ? 0.6f : (float)i / (treeClusterCount - 1);
            specs.Add(new ClusterSpec
            {
                type = "tree",
                dNorm = dNorm,
                isNearTree = i == 0,
                budget = i == 0 ? nearTrees : farTreeBudgets[i - 1]
            });
        }

        // 돌: 군락 0(가장 가까움)은 최근접 거리 상한을 지키도록 중심 거리 제한
        var stoneBudgets = SplitBudgetByDistance(stoneTotal, stoneClusterCount);
        for (int i = 0; i < stoneClusterCount; i++)
        {
            float dNorm = stoneClusterCount == 1 ? 0.3f : (float)i / (stoneClusterCount - 1);
            specs.Add(new ClusterSpec
            {
                type = "stone",
                dNorm = dNorm,
                budget = stoneBudgets[i],
                maxCenterDist = i == 0 ? Mathf.Max(ExclusionRadius + 2f, stoneNearestMaxDist - 2f) : float.MaxValue
            });
        }

        // 베리
        var berryBudgets = SplitBudgetByDistance(berryTotal, berryClusterCount);
        for (int i = 0; i < berryClusterCount; i++)
        {
            float dNorm = berryClusterCount == 1 ? 0.5f : (float)i / (berryClusterCount - 1);
            specs.Add(new ClusterSpec { type = "berry", dNorm = dNorm, budget = berryBudgets[i] });
        }

        // 부채꼴 분할로 방향 분리
        Shuffle(specs);
        float rot = (float)(rng.NextDouble() * 360.0);
        float sector = 360f / specs.Count;
        for (int i = 0; i < specs.Count; i++)
        {
            specs[i].angleMin = rot + i * sector + sector * 0.15f;
            specs[i].angleMax = rot + (i + 1) * sector - sector * 0.15f;
        }

        float maxDist = ExclusionRadius + clusterRadiusMax + 6f;
        for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
                if (roundTrip[x, z])
                    maxDist = Mathf.Max(maxDist, Vector2.Distance(new Vector2(x, z), new Vector2(start.x, start.y)));

        foreach (var spec in specs)
            PlaceOneCluster(start, spec, maxDist);

        // 보충 패스: 목표 총량 미달분을 같은 유형 주변에 채워 군락감을 유지하며 총량 보장
        int lackNear = (nearTreeBase - nearTreeJitter) - nearTreePlaced;
        if (lackNear > 0 && hasNearTreeCenter)
            nearTreePlaced += TopUpAround(start, "tree", lackNear, nearTreeCenter, 6);

        int CountOf(string id) => placements.Count(p => p.id == id);
        TopUpType(start, "tree", treeTotal - CountOf("tree"));
        TopUpType(start, "stone", stoneTotal - CountOf("stone"));
        TopUpType(start, "berry", berryTotal - CountOf("berry"));
    }

    // 지정 중심 주변에 부족분 배치
    int TopUpAround(Vector2Int start, string id, int deficit, Vector2Int center, int radius)
    {
        // 금지 구역은 군락 모드의 나무/돌/베리에만 적용 (잔가지는 금지 구역 안 허용)
        bool applyExclusion = scatterMode == ScatterMode.Clusters && id != "stick";
        float R = ExclusionRadius;
        int placed = 0, guard = 0;
        while (placed < deficit && guard++ < 800)
        {
            int x = center.x + rng.Next(-radius, radius + 1);
            int z = center.y + rng.Next(-radius, radius + 1);
            if ((new Vector2Int(x, z) - start).sqrMagnitude < 2 * 2) continue;   // 화로 바로 옆은 회피
            if (applyExclusion && (new Vector2Int(x, z) - start).sqrMagnitude <= R * R) continue;
            if (!CanPlaceResource(x, z)) continue;
            if (HasNeighborPlacement(x, z, 1)) continue;
            AddPlacement(id, x, z, rng.Next(4) * 90f);
            placed++;
        }
        if (placed > 0)
            Debug.Log($"[MapGenerator] 근접 보충: {id} {placed}개 ({center.x},{center.y} 주변)");
        return placed;
    }

    // 유형별 총량 부족분을 같은 유형 배치물 근처(80%) 또는 임의 지점(20%)에 채움
    void TopUpType(Vector2Int start, string id, int deficit)
    {
        if (deficit <= 0) return;
        float R = ExclusionRadius;
        var same = placements.Where(p => p.id == id).Select(p => new Vector2Int(p.x, p.z)).ToList();
        int placed = 0, guard = 0;
        while (placed < deficit && guard++ < 2000)
        {
            int x, z;
            if (same.Count > 0 && rng.NextDouble() < 0.8)
            {
                var b = same[rng.Next(same.Count)];
                x = b.x + rng.Next(-4, 5);
                z = b.y + rng.Next(-4, 5);
            }
            else
            {
                x = rng.Next(width);
                z = rng.Next(depth);
            }
            if ((new Vector2Int(x, z) - start).sqrMagnitude <= R * R) continue;
            if (!CanPlaceResource(x, z)) continue;
            if (HasNeighborPlacement(x, z, 1)) continue;
            AddPlacement(id, x, z, rng.Next(4) * 90f);
            same.Add(new Vector2Int(x, z));
            placed++;
        }
        if (placed > 0)
            Debug.Log($"[MapGenerator] 보충 배치: {id} {placed}개");
        if (placed < deficit)
            Debug.LogWarning($"[MapGenerator] {id} 총량 {deficit - placed}개 미달. Crag Threshold 상향 또는 맵 확대 검토.");
    }

    // 거리 가중 분배: 먼 군락일수록 큰 몫. 합계가 정확히 total이 되도록 보정
    int[] SplitBudgetByDistance(int total, int count)
    {
        var result = new int[Mathf.Max(0, count)];
        if (count <= 0 || total <= 0) return result;

        var w = new float[count];
        float sum = 0f;
        for (int i = 0; i < count; i++)
        {
            float dNorm = count == 1 ? 0.6f : (float)i / (count - 1);
            w[i] = 0.7f + 0.6f * dNorm;
            sum += w[i];
        }
        int acc = 0;
        for (int i = 0; i < count; i++)
        {
            result[i] = Mathf.FloorToInt(total * w[i] / sum);
            acc += result[i];
        }
        result[count - 1] += total - acc;
        return result;
    }

    void PlaceOneCluster(Vector2Int start, ClusterSpec spec, float maxDist)
    {
        // 적응형 반경: 간격 1 규칙(밀도 ~1개/4칸)에서 예산이 들어가는 최소 반경을 보장
        float cr = Mathf.Lerp(clusterRadiusMin, clusterRadiusMax, spec.dNorm);
        float needed = 2f * Mathf.Sqrt(Mathf.Max(1, spec.budget) / Mathf.PI) + 0.6f;
        cr = Mathf.Max(cr, needed);

        float minDist = ExclusionRadius + cr + 1f;
        float distCap = Mathf.Min(maxDist, spec.maxCenterDist);

        // 경계 마진: 군락 원이 맵 밖으로 잘리지 않게 중심을 안쪽으로 제한
        int mg = Mathf.CeilToInt(cr * 0.7f);
        bool OkCenter(int x, int z) =>
            x >= mg && x < width - mg && z >= mg && z < depth - mg && resourceAllowed[x, z];

        // 목표 거리 상한은 '유효한 중심 후보' 기준으로 재계산
        // (마진 박스 밖 맵 모서리를 목표 거리로 잡아 300회를 허비하는 것 방지)
        float validMax = 0f;
        for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
                if (OkCenter(x, z))
                    validMax = Mathf.Max(validMax, Vector2.Distance(new Vector2(x, z), new Vector2(start.x, start.y)));
        distCap = Mathf.Max(Mathf.Min(distCap, validMax), minDist);

        float target = Mathf.Lerp(minDist, distCap, Mathf.Lerp(0.05f, 0.95f, spec.dNorm));
        Vector2Int center = default;
        bool found = false;

        for (int attempt = 0; attempt < 300 && !found; attempt++)
        {
            float ang = Mathf.Lerp(spec.angleMin, spec.angleMax, (float)rng.NextDouble()) * Mathf.Deg2Rad;
            float d = target * (0.85f + 0.3f * (float)rng.NextDouble());
            d = Mathf.Clamp(d, minDist, distCap);
            int x = start.x + Mathf.RoundToInt(Mathf.Cos(ang) * d);
            int z = start.y + Mathf.RoundToInt(Mathf.Sin(ang) * d);
            if (!InBounds(x, z) || !OkCenter(x, z)) continue;
            center = new Vector2Int(x, z);
            found = true;
        }
        if (!found)
        {
            // 폴백: 각도 제한 해제, 전 방향으로 300회 재시도 (방향 분리보다 총량 보장이 우선)
            for (int attempt = 0; attempt < 300 && !found; attempt++)
            {
                float ang = (float)(rng.NextDouble() * 2.0 * Math.PI);
                float d = target * (0.85f + 0.3f * (float)rng.NextDouble());
                d = Mathf.Clamp(d, minDist, distCap);
                int x = start.x + Mathf.RoundToInt(Mathf.Cos(ang) * d);
                int z = start.y + Mathf.RoundToInt(Mathf.Sin(ang) * d);
                if (!InBounds(x, z) || !OkCenter(x, z)) continue;
                center = new Vector2Int(x, z);
                found = true;
            }
            if (found)
                Debug.Log($"[MapGenerator] {spec.type} 군락(dNorm {spec.dNorm:0.0}): 부채꼴 내 배치 실패 → 전 방향 폴백으로 ({center.x},{center.y})에 배치. 방향 분리가 일부 깨질 수 있음.");
        }
        if (!found)
        {
            Debug.LogWarning($"[MapGenerator] {spec.type} 군락(dNorm {spec.dNorm:0.0}) 중심을 폴백으로도 못 찾아 건너뜀 → 총량 미달 가능. Crag Threshold 상향(바위 감소)이나 맵 확대 검토.");
            return;
        }

        float R = ExclusionRadius;
        int placed = 0;
        // 1차: 기본 반경 / 2차: 미달 시 반경 1.5배로 확장 채우기
        foreach (float grow in new[] { 1f, 1.5f })
        {
            float rr = cr * grow;
            var cells = new List<Vector2Int>();
            int r = Mathf.CeilToInt(rr);
            for (int z = center.y - r; z <= center.y + r; z++)
                for (int x = center.x - r; x <= center.x + r; x++)
                {
                    if (!InBounds(x, z)) continue;
                    if ((new Vector2Int(x, z) - center).sqrMagnitude > rr * rr) continue;
                    cells.Add(new Vector2Int(x, z));
                }
            Shuffle(cells);

            foreach (var c in cells)
            {
                if (placed >= spec.budget) break;
                if ((c - start).sqrMagnitude <= R * R) continue;
                if (!CanPlaceResource(c.x, c.y)) continue;
                if (HasNeighborPlacement(c.x, c.y, 1)) continue;

                AddPlacement(spec.type, c.x, c.y, rng.Next(4) * 90f);
                placed++;
            }
            if (placed >= spec.budget) break;
        }

        if (spec.isNearTree)
        {
            nearTreePlaced = placed;
            nearTreeCenter = center;
            hasNearTreeCenter = true;
        }

        if (placed < spec.budget)
            Debug.Log($"[MapGenerator] {spec.type} 군락({center.x},{center.y}): 예산 {spec.budget} 중 {placed}개 배치, 부족분은 보충 패스로 이월 (군락 반경 {cr:0.0}).");
    }

    // 최근접 돌 거리 상한 보정: 군락 결과가 상한을 넘으면 상한 안쪽에 돌 1개 보정 배치
    void EnsureNearbyStone(Vector2Int start)
    {
        var stones = placements.Where(p => p.id == "stone").ToList();
        float nearest = stones.Count == 0
            ? float.MaxValue
            : stones.Min(p => Vector2.Distance(new Vector2(p.x, p.z), new Vector2(start.x, start.y)));
        if (nearest <= stoneNearestMaxDist) return;

        int guard = 0;
        while (guard++ < 500)
        {
            int x = start.x + rng.Next(-(int)stoneNearestMaxDist, (int)stoneNearestMaxDist + 1);
            int z = start.y + rng.Next(-(int)stoneNearestMaxDist, (int)stoneNearestMaxDist + 1);
            float d = Vector2.Distance(new Vector2(x, z), new Vector2(start.x, start.y));
            if (d <= ExclusionRadius || d > stoneNearestMaxDist) continue;
            if (!CanPlaceResource(x, z)) continue;
            if (HasNeighborPlacement(x, z, 1)) continue;
            AddPlacement("stone", x, z, rng.Next(4) * 90f);
            Debug.Log($"[MapGenerator] 최근접 돌 상한({stoneNearestMaxDist:0}) 초과(기존 {nearest:0.0}) → ({x},{z})에 돌 1개 보정 배치.");
            return;
        }
        Debug.LogWarning($"[MapGenerator] 최근접 돌 보정 배치 실패. 금지 구역 R({ExclusionRadius:0.0})과 상한({stoneNearestMaxDist:0}) 사이가 좁거나 지형 협소. Exclusion Factor 하향 또는 상한 상향 검토.");
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

    bool CanPlaceResource(int x, int z)
    {
        return InBounds(x, z)
            && resourceAllowed[x, z]
            && !occupied[x, z]
            && !ramps[x, z]
            && !HasRampNeighbor(x, z);
    }

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
                if (!CanPlaceResource(x, z)) continue;
                if (dist[x, z] < maxDist * 7 / 10) continue;
                int score = dist[x, z] + levels[x, z] * width;
                if (score > bestScore) { bestScore = score; best = new Vector2Int(x, z); }
            }

        if (bestScore == int.MinValue)
        {
            Debug.LogWarning("[MapGenerator] hearth_site 후보를 못 찾았습니다. 임의 도달 가능 칸에 배치.");
            for (int z = 0; z < depth && bestScore == int.MinValue; z++)
                for (int x = 0; x < width && bestScore == int.MinValue; x++)
                    if (CanPlaceResource(x, z) && dist[x, z] > (int)ExclusionRadius)
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
            var visF = BfsWithPlacements(start);
            var visR = BfsReturnWithPlacements(start);
            bool Ok(int x, int z) => visF[x, z] && visR[x, z];

            var isolated = new List<Vector2Int>();
            for (int z = 0; z < depth; z++)
                for (int x = 0; x < width; x++)
                    if (!occupied[x, z] && roundTrip[x, z] && !Ok(x, z))
                        isolated.Add(new Vector2Int(x, z));
            if (isolated.Count == 0) return removed;

            MapPlacement culprit = null;
            // 잔가지는 초기 자원 보장 대상이므로 마지막 순위로 걷어냄
            var byPriority = placements
                .Where(p => RemovableIds.Contains(p.id))
                .OrderBy(p => p.id == "tree" ? 0 : p.id == "stone" ? 1 : p.id == "berry" ? 2 : 3);
            foreach (var p in byPriority)
            {
                bool touchVis = false, touchIso = false;
                for (int d = 0; d < 4; d++)
                {
                    int nx = p.x + DX[d], nz = p.z + DZ[d];
                    if (!InBounds(nx, nz) || occupied[nx, nz]) continue;
                    if (!PassableFrom(p.x, p.z, nx, nz) && !PassableFrom(nx, nz, p.x, p.z)) continue;
                    if (Ok(nx, nz)) touchVis = true;
                    else if (roundTrip[nx, nz]) touchIso = true;
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

    // 배치물 차단 포함 전진 BFS (갈 수 있는가)
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
                if (!PassableFrom(c.x, c.y, nx, nz)) continue;
                vis[nx, nz] = true;
                queue.Enqueue(new Vector2Int(nx, nz));
            }
        }
        return vis;
    }

    // 배치물 차단 포함 역방향 BFS (돌아올 수 있는가)
    bool[,] BfsReturnWithPlacements(Vector2Int start)
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
                if (!PassableFrom(nx, nz, c.x, c.y)) continue;
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

        // 인접 층 차이 ≤ 1
        for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
                for (int d = 0; d < 2; d++)
                {
                    int nx = x + (d == 0 ? 1 : 0);
                    int nz = z + (d == 0 ? 0 : 1);
                    if (!InBounds(nx, nz)) continue;
                    if (Mathf.Abs(levels[x, z] - levels[nx, nz]) >= 2)
                        Fail($"층 2단 절벽 ({x},{z})↔({nx},{nz}): 계단화 실패");
                }

        // 경사로: 묶음이 "길이 N × 폭 1~rampWidth" 직사각형이어야 함, +1 이웃 보유
        foreach (var group in FindRampGroups())
        {
            if (!GroupHasUpNeighbor(group))
                Fail($"무효 경사로 체인 ({group[0].x},{group[0].y}) 외 {group.Count - 1}칸: 한 단 높은 이웃 없음");

            int minX = group.Min(c => c.x), maxX = group.Max(c => c.x);
            int minZ = group.Min(c => c.y), maxZ = group.Max(c => c.y);
            int dx = maxX - minX + 1, dz = maxZ - minZ + 1;
            bool rect = group.Count == dx * dz;
            bool dims = (dx == rampLength && dz <= rampWidth) || (dz == rampLength && dx <= rampWidth);
            if (!rect || !dims)
                Fail($"경사로 묶음 ({group[0].x},{group[0].y}): {group.Count}칸 {dx}x{dz} (규격: 길이 {rampLength} × 폭 ≤{rampWidth} 직사각형)");
        }

        // placements 공통
        var counts = new Dictionary<string, int>();
        var cells = new HashSet<(int, int)>();
        foreach (var p in placements)
        {
            if (!ValidIds.Contains(p.id)) Fail($"알 수 없는 id '{p.id}' ({p.x},{p.z})");
            if (!InBounds(p.x, p.z)) { Fail($"범위 밖 placement ({p.x},{p.z})"); continue; }
            if (!cells.Add((p.x, p.z))) Fail($"같은 칸에 배치물 중복 ({p.x},{p.z})");
            if (!roundTrip[p.x, p.z]) Fail($"왕복 불가 칸 위 placement '{p.id}' ({p.x},{p.z})");
            counts[p.id] = counts.GetValueOrDefault(p.id) + 1;
        }

        if (counts.GetValueOrDefault("hearth") != 1) Fail("hearth가 정확히 1개가 아님");
        if (counts.GetValueOrDefault("hearth_site") != 1) Fail("hearth_site가 정확히 1개가 아님");

        // 고원 위 자원 → 경사로 2개 보장 지역만
        foreach (var p in placements)
        {
            if (!RemovableIds.Contains(p.id) || !InBounds(p.x, p.z)) continue;
            if (levels[p.x, p.z] >= 1 && !resourceAllowed[p.x, p.z])
                Fail($"경사로 2개 미보장 고원 위 자원 '{p.id}' ({p.x},{p.z})");
        }

        if (scatterMode == ScatterMode.Clusters)
        {
            // 금지 구역: 안쪽엔 잔가지만
            float R = ExclusionRadius;
            foreach (var p in placements)
            {
                if (!Near(p, start, Mathf.FloorToInt(R))) continue;
                if (p.id == "tree" || p.id == "stone" || p.id == "berry")
                    Fail($"금지 구역(R={R:0.0}) 안에 자원 '{p.id}' ({p.x},{p.z})");
            }

            // 목표 총량 검사 — 미달 시 조정할 파라미터 안내 포함
            int trees = counts.GetValueOrDefault("tree");
            int berries = counts.GetValueOrDefault("berry");
            int stoneCount = counts.GetValueOrDefault("stone");
            int sticks = counts.GetValueOrDefault("stick");

            if (trees < treeTotalMin || trees > treeTotalMax)
                Fail($"나무 {trees}그루 (목표 {treeTotalMin}~{treeTotalMax}). 미달이면 Cluster Radius/Tree Clusters 확대 또는 Crag Threshold 상향으로 배치 지형 확보");
            if (nearTreePlaced < nearTreeBase - nearTreeJitter || nearTreePlaced > nearTreeBase + nearTreeJitter)
                Fail($"가까운 나무 군락 {nearTreePlaced}그루 (목표 {nearTreeBase}±{nearTreeJitter}). 미달이면 Cluster Radius Min 확대 검토");
            if (berries < berryTotalMin || berries > berryTotalMax)
                Fail($"베리 {berries}개 (목표 {berryTotalMin}~{berryTotalMax})");
            if (stoneCount < stoneTotalMin)
                Fail($"돌 {stoneCount}개 (목표 {stoneTotalMin} 이상). Stone Clusters 확대 또는 지형 확인");
            if (sticks < innerSticksMin)
                Fail($"잔가지 {sticks}개 (초기 목표 {innerSticksMin} 이상)");

            // 최근접 돌 거리
            var stones = placements.Where(p => p.id == "stone").ToList();
            if (stones.Count > 0)
            {
                float nearest = stones.Min(p => Vector2.Distance(new Vector2(p.x, p.z), new Vector2(start.x, start.y)));
                if (nearest > stoneNearestMaxDist)
                    Fail($"최근접 돌 {nearest:0.0}타일 (상한 {stoneNearestMaxDist:0.0}). Exclusion Factor 하향 또는 상한 상향 검토");
            }
        }
        else
        {
            // 예전 방식: 규격의 "시작 지점 주변 나뭇가지·돌" 제약만 확인
            bool stickNear = placements.Any(p => p.id == "stick" && Near(p, start, startClearRadius + 2));
            bool stoneNear = placements.Any(p => p.id == "stone" && Near(p, start, startClearRadius + 2));
            if (!stickNear) Fail("시작 지점 근처에 stick 없음");
            if (!stoneNear) Fail("시작 지점 근처에 stone 없음");
            if (counts.GetValueOrDefault("stone") == 0) Fail("맵에 돌이 하나도 없음 (도끼 제작 불가)");
        }

        // 연결성 이중 검사 (배치물 차단 포함): 가는 길 + 돌아오는 길
        var visF = BfsWithPlacements(start);
        var visR = BfsReturnWithPlacements(start);
        var site = placements.FirstOrDefault(p => p.id == "hearth_site");
        if (site != null && InBounds(site.x, site.z))
        {
            // 화로 터 옆에 서서 왕복할 수 있어야 함 (터 칸 자체는 배치물이라 막힘)
            bool reached = false;
            for (int d = 0; d < 4 && !reached; d++)
            {
                int nx = site.x + DX[d], nz = site.z + DZ[d];
                if (InBounds(nx, nz) && !occupied[nx, nz] && visF[nx, nz] && visR[nx, nz]) reached = true;
            }
            if (!reached) Fail("hearth ↔ hearth_site 왕복 보행 경로 없음 (배치물 차단 포함)");
        }

        // 지형 원웨이 잔존 (탈출 경사로 보정 실패분) = 소프트락 위험
        int terrainOneWay = 0;
        for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
                if (reachable[x, z] && !canReturn[x, z]) terrainOneWay++;
        if (terrainOneWay > 0) Fail($"원웨이 지형 {terrainOneWay}칸 잔존 (뛰어내리면 못 돌아옴). 시드 변경 권장");

        // 배치물이 만든 고립/원웨이
        int pinchIsolated = 0;
        for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
                if (!occupied[x, z] && roundTrip[x, z] && !(visF[x, z] && visR[x, z])) pinchIsolated++;
        if (pinchIsolated > 0) Fail($"배치물로 인한 고립/원웨이 {pinchIsolated}칸 잔존");

        // 배경(전진 도달 불가)은 허용 — 정보 로그
        int backgroundCells = 0;
        for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
                if (!reachable[x, z]) backgroundCells++;
        string nearInfo = scatterMode == ScatterMode.Clusters ? $" (가까운 나무 군락 {nearTreePlaced})" : "";
        Debug.Log($"[MapGenerator/검증] 배경(도달 불가) {backgroundCells}칸 — 허용. " +
                  $"총량: 나무 {counts.GetValueOrDefault("tree")} / 베리 {counts.GetValueOrDefault("berry")} / " +
                  $"돌 {counts.GetValueOrDefault("stone")} / 잔가지 {counts.GetValueOrDefault("stick")}{nearInfo}");

        if (ok) Debug.Log("[MapGenerator/검증] 체크리스트 전 항목 통과");
        return ok;
    }

    bool Near(MapPlacement p, Vector2Int c, int r)
        => (new Vector2Int(p.x, p.z) - c).sqrMagnitude <= r * r;

    // ---- 저장 ----
    // decorations는 규격 외 확장 필드. JsonUtility 로더는 모르는 필드를 무시하므로
    // 메인 쪽이 지원하기 전에도 안전함. 지원 시 MapData에 `public MapPlacement[] decorations;`
    // 한 줄 추가하고, 비차단(통행·상호작용 없음) 순수 장식으로 처리하기로 협의 필요.
    [Serializable]
    class MapDataOut
    {
        public float cellSize;
        public int width;
        public int depth;
        public string[] levels;
        public string[] ramps;
        public MapPlacement[] placements;
        public MapPlacement[] decorations;
    }

    void WriteJson()
    {
        var data = new MapDataOut
        {
            cellSize = 1f,
            width = width,
            depth = depth,
            levels = RowsFrom((x, z) => (char)('0' + levels[x, z])),
            ramps = RowsFrom((x, z) => ramps[x, z] ? '/' : '.'),
            placements = placements.ToArray(),
            decorations = emitDecorations ? decorations.ToArray() : Array.Empty<MapPlacement>()
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