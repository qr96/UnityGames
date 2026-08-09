using UnityEngine;

// 월드 격자. 1칸 = cellSize(기본 1m).
// 담당: 좌표 변환 / 칸 점유(논리) / 높이 층·절벽 통행 판정.
// 이동은 자유(연속). 격자는 층·절벽만 막고, 나무·바위 같은 개체의 물리적 차단은 콜라이더가 맡는다.
// 칸 점유(GridOccupant)는 '그 칸에 설치할 수 있는가'와 이후 경로 탐색을 위한 정보다.
public class WorldGrid : MonoBehaviour
{
    public static WorldGrid Instance { get; private set; }

    [Header("규격")]
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private int width = 60;
    [SerializeField] private int depth = 60;
    [Tooltip("격자 (0,0)칸의 월드 좌표")]
    [SerializeField] private Vector3 origin = Vector3.zero;

    [Header("높이 층")]
    [Tooltip("한 층의 월드 높이(m)")]
    [SerializeField] private float levelHeight = 1f;

    [Header("규칙")]
    [Tooltip("켜면 층이 달라도 온기가 넘어간다. 끄면 절벽 위아래는 서로 데우지 않음")]
    [SerializeField] private bool warmthCrossesLevels = false;

    [Tooltip("켜면 격자를 점유한 칸 자체가 통행 불가가 된다(네모난 차단). " +
             "기본은 꺼짐 — 물리적 차단은 콜라이더가 맡고, 격자 점유는 설치 가능 여부·경로 정보로만 쓴다")]
    [SerializeField] private bool blockMovementOnOccupied = false;

    [Header("표시")]
    [SerializeField] private bool drawGizmo = true;

    // 지형이 바뀌면 알림 — 지형 렌더러가 해당 청크만 다시 굽는다
    public event System.Action<Vector2Int> OnCellChanged;
    public event System.Action OnGridReloaded;

    private GameObject[,] occupants;
    private int[,] levels;
    private bool[,] ramps;

    public float CellSize => cellSize;
    public int Width => width;
    public int Depth => depth;
    public Vector3 Origin => origin;
    public float LevelHeight => levelHeight;
    public bool WarmthCrossesLevels => warmthCrossesLevels;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        Allocate();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Allocate()
    {
        occupants = new GameObject[width, depth];
        levels = new int[width, depth];
        ramps = new bool[width, depth];
    }

    // ---- 맵 파일 적용 ----
    public void ApplyMapData(MapData data)
    {
        if (data == null) return;

        cellSize = data.cellSize > 0f ? data.cellSize : cellSize;
        width = Mathf.Max(1, data.width);
        depth = Mathf.Max(1, data.depth);
        Allocate();

        ReadRows(data.levels, (x, z, c) => levels[x, z] = (c >= '0' && c <= '9') ? c - '0' : 0);
        ReadRows(data.ramps, (x, z, c) => ramps[x, z] = (c == '/'));

        ValidateRamps();
        OnGridReloaded?.Invoke();
    }

    // 경사로가 '낮은 칸'에 놓였는지 검사. 높은 쪽에 두면 경사면이 생기지 않는다.
    private void ValidateRamps()
    {
        int bad = 0;
        for (int x = 0; x < width; x++)
            for (int z = 0; z < depth; z++)
            {
                Vector2Int c = new Vector2Int(x, z);
                if (!ramps[x, z]) continue;
                if (TryGetRampRise(c, out _)) continue;

                bad++;
                Debug.LogWarning($"[격자] 경사로 ({x},{z}): 오를 수 있는 축을 찾지 못함 — " +
                                 $"현재 층 {levels[x, z]}. 경사로는 낮은 쪽 칸에 두고, " +
                                 "런의 한쪽 끝은 한 단 높은 칸, 반대쪽 끝은 같은 층(진입로)이어야 함");
            }
        if (bad > 0) Debug.LogWarning($"[격자] 잘못 놓인 경사로 {bad}개 — 경사면이 생기지 않음");
    }

    private void ReadRows(string[] rows, System.Action<int, int, char> set)
    {
        if (rows == null) return;
        for (int z = 0; z < depth && z < rows.Length; z++)
        {
            string row = rows[z];
            if (string.IsNullOrEmpty(row)) continue;
            for (int x = 0; x < width && x < row.Length; x++)
                set(x, z, row[x]);
        }
    }

    // ---- 칸 수정 (변경 시 해당 칸만 알림) ----
    public void SetLevel(Vector2Int cell, int level)
    {
        if (!InBounds(cell) || levels[cell.x, cell.y] == level) return;
        levels[cell.x, cell.y] = level;
        OnCellChanged?.Invoke(cell);
    }

    public void SetRamp(Vector2Int cell, bool value)
    {
        if (!InBounds(cell) || ramps[cell.x, cell.y] == value) return;
        ramps[cell.x, cell.y] = value;
        OnCellChanged?.Invoke(cell);
    }

    // ---- 좌표 변환 ----
    public Vector2Int WorldToCell(Vector3 world)
    {
        Vector3 local = world - origin;
        return new Vector2Int(
            Mathf.FloorToInt(local.x / cellSize),
            Mathf.FloorToInt(local.z / cellSize));
    }

    public Vector3 CellToWorld(Vector2Int cell)
        => origin + new Vector3((cell.x + 0.5f) * cellSize, HeightAt(cell), (cell.y + 0.5f) * cellSize);

    public Vector3 CellToWorldCenter(Vector2Int cell, Vector2Int footprint)
    {
        Vector2Int f = Max1(footprint);
        return origin + new Vector3(
            (cell.x + f.x * 0.5f) * cellSize,
            HeightAt(cell),
            (cell.y + f.y * 0.5f) * cellSize);
    }

    public bool InBounds(Vector2Int cell)
        => cell.x >= 0 && cell.y >= 0 && cell.x < width && cell.y < depth;

    // ---- 층 / 통행 ----
    public int GetLevel(Vector2Int cell) => InBounds(cell) ? levels[cell.x, cell.y] : 0;
    public bool IsRamp(Vector2Int cell) => InBounds(cell) && ramps[cell.x, cell.y];
    // 배치물이 점유해 막힌 칸인지
    public bool IsBlocked(Vector2Int cell)
        => !InBounds(cell) || (blockMovementOnOccupied && occupants[cell.x, cell.y] != null);

    public float HeightAt(Vector2Int cell) => origin.y + GetLevel(cell) * levelHeight;

    // 경사로 정보. 여러 칸이 한 줄로 이어지면 그 길이에 걸쳐 한 층을 오른다.
    //  dir   = 올라가는 방향
    //  index = 이 칸이 런에서 몇 번째인지(0 = 가장 낮은 쪽)
    //  count = 런의 전체 칸 수
    public bool TryGetRampInfo(Vector2Int cell, out Vector2Int dir, out int index, out int count)
    {
        dir = Vector2Int.zero; index = 0; count = 1;
        if (!IsRamp(cell)) return false;

        int my = GetLevel(cell);
        Vector2Int[] dirs =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, 1), new Vector2Int(0, -1),
        };

        const int maxRun = 16;

        // 1순위: 오르는 쪽 끝이 +1층이고, 반대쪽 끝이 같은 층(열린 진입로)인 축.
        //         노치형(절벽 안으로 파인 형태)에서는 양옆도 +1층이라 이 조건으로 벽과 축을 가른다.
        // 2순위: 진입로 조건이 맞지 않으면(막다른 형태) 오르는 조건만으로 판단.
        Vector2Int fallbackDir = Vector2Int.zero;
        int fallbackIndex = 0, fallbackCount = 1;
        bool hasFallback = false;

        for (int i = 0; i < dirs.Length; i++)
        {
            Vector2Int d = dirs[i];

            // 이 방향으로 이어진 같은 층 경사로 칸 수(자기 포함)
            int forward = 1;
            while (forward < maxRun)
            {
                Vector2Int n = cell + d * forward;
                if (!InBounds(n) || !ramps[n.x, n.y] || levels[n.x, n.y] != my) break;
                forward++;
            }

            // 런의 끝 다음 칸이 한 단 높아야 오르는 방향
            Vector2Int top = cell + d * forward;
            if (!InBounds(top) || GetLevel(top) != my + 1) continue;

            // 반대쪽으로 이어진 칸 수
            int back = 0;
            while (back < maxRun)
            {
                Vector2Int n = cell - d * (back + 1);
                if (!InBounds(n) || !ramps[n.x, n.y] || levels[n.x, n.y] != my) break;
                back++;
            }

            if (!hasFallback)
            {
                fallbackDir = d; fallbackIndex = back; fallbackCount = back + forward;
                hasFallback = true;
            }

            // 진입로 검사: 런의 반대쪽 끝 바깥이 같은 층이어야 축이다
            Vector2Int entry = cell - d * (back + 1);
            if (!InBounds(entry) || GetLevel(entry) != my) continue;

            dir = d;
            index = back;
            count = back + forward;
            return true;
        }

        if (hasFallback)
        {
            dir = fallbackDir; index = fallbackIndex; count = fallbackCount;
            return true;
        }

        return false;
    }

    public bool TryGetRampRise(Vector2Int cell, out Vector2Int dir)
        => TryGetRampInfo(cell, out dir, out _, out _);

    // 칸의 모서리별 지표면 높이. corner: 0=SW, 1=NW, 2=NE, 3=SE
    // 경사로 칸이면 올라가는 쪽 두 모서리가 한 단계 높다.
    public float SurfaceHeightAtCorner(Vector2Int cell, int corner)
    {
        float baseY = HeightAt(cell);
        if (!TryGetRampInfo(cell, out Vector2Int dir, out int index, out int count)) return baseY;

        float step = levelHeight / Mathf.Max(1, count);
        float low = baseY + index * step;
        float high = low + step;

        bool isHigh;
        if (dir.x > 0) isHigh = (corner == 2 || corner == 3); // NE, SE
        else if (dir.x < 0) isHigh = (corner == 0 || corner == 1); // SW, NW
        else if (dir.y > 0) isHigh = (corner == 1 || corner == 2); // NW, NE
        else isHigh = (corner == 0 || corner == 3); // SW, SE

        return isHigh ? high : low;
    }

    // 월드 지점의 실제 지면 높이. 경사로 칸에서는 칸 안 위치에 따라 보간된다.
    public float SampleHeight(Vector3 world)
    {
        Vector2Int cell = WorldToCell(world);
        float baseY = HeightAt(cell);

        if (!TryGetRampInfo(cell, out Vector2Int dir, out int index, out int count)) return baseY;

        // 칸 내부 진행도(0=낮은 쪽, 1=높은 쪽)
        Vector3 local = world - origin;
        float fx = Mathf.Repeat(local.x / cellSize, 1f);
        float fz = Mathf.Repeat(local.z / cellSize, 1f);

        float t;
        if (dir.x > 0) t = fx;
        else if (dir.x < 0) t = 1f - fx;
        else if (dir.y > 0) t = fz;
        else t = 1f - fz;

        // 런 전체가 한 층을 오르므로 칸별 구간만큼만 상승
        float progress = (index + Mathf.Clamp01(t)) / Mathf.Max(1, count);
        return baseY + progress * levelHeight;
    }

    // 그 칸에 설 수 있는지
    public bool CanStand(Vector2Int cell)
    {
        if (!InBounds(cell)) return false;
        if (blockMovementOnOccupied && occupants[cell.x, cell.y] != null) return false;
        return true;
    }

    // 이웃 칸으로 넘어갈 수 있는지 (동물의 숲식 절벽 규칙)
    //  - 같은 층: 통행 가능
    //  - 1층 차이: 경사로를 '경사 축 방향으로' 지날 때만 가능
    //             (노치형에서 양옆 벽을 타고 오르는 것을 막는다)
    //  - 2층 이상: 불가
    public bool CanMoveBetween(Vector2Int from, Vector2Int to)
    {
        if (from == to) return CanStand(to);
        if (!CanStand(to) || !InBounds(from)) return false;

        int diff = GetLevel(to) - GetLevel(from);
        if (diff == 0) return true;
        if (Mathf.Abs(diff) > 1) return false;

        if (diff > 0)
        {
            // 올라가기: 지금 칸이 경사로이고, 목적지가 그 경사 축의 위쪽이어야 한다
            if (!TryGetRampInfo(from, out Vector2Int upDir, out _, out _)) return false;
            return to == from + upDir;
        }
        else
        {
            // 내려가기: 목적지가 경사로이고, 지금 칸이 그 경사 축의 위쪽이어야 한다
            if (!TryGetRampInfo(to, out Vector2Int downDir, out _, out _)) return false;
            return from == to + downDir;
        }
    }

    // 월드 좌표 기준 이동 가능 판정
    public bool CanMoveToWorld(Vector3 fromWorld, Vector3 toWorld)
        => CanMoveBetween(WorldToCell(fromWorld), WorldToCell(toWorld));

    // ---- 점유 ----
    public bool IsFree(Vector2Int cell, Vector2Int footprint, GameObject ignore = null)
    {
        Vector2Int f = Max1(footprint);
        for (int x = 0; x < f.x; x++)
            for (int z = 0; z < f.y; z++)
            {
                Vector2Int c = new Vector2Int(cell.x + x, cell.y + z);
                if (!InBounds(c)) return false;
                GameObject o = occupants[c.x, c.y];
                if (o != null && o != ignore) return false;
            }
        return true;
    }

    public bool Occupy(Vector2Int cell, Vector2Int footprint, GameObject owner)
    {
        if (!IsFree(cell, footprint, owner)) return false;
        Vector2Int f = Max1(footprint);
        for (int x = 0; x < f.x; x++)
            for (int z = 0; z < f.y; z++)
                occupants[cell.x + x, cell.y + z] = owner;
        return true;
    }

    public void Free(Vector2Int cell, Vector2Int footprint, GameObject owner)
    {
        if (occupants == null) return;
        Vector2Int f = Max1(footprint);
        for (int x = 0; x < f.x; x++)
            for (int z = 0; z < f.y; z++)
            {
                Vector2Int c = new Vector2Int(cell.x + x, cell.y + z);
                if (!InBounds(c)) continue;
                if (occupants[c.x, c.y] == owner) occupants[c.x, c.y] = null;
            }
    }

    public GameObject GetOccupant(Vector2Int cell)
        => InBounds(cell) && occupants != null ? occupants[cell.x, cell.y] : null;

    private static Vector2Int Max1(Vector2Int v)
        => new Vector2Int(Mathf.Max(1, v.x), Mathf.Max(1, v.y));

    private void OnDrawGizmos()
    {
        if (!drawGizmo) return;

        // 격자선
        Gizmos.color = new Color(1f, 1f, 1f, 0.10f);
        float w = width * cellSize, d = depth * cellSize;
        for (int x = 0; x <= width; x++)
        {
            Vector3 a = origin + new Vector3(x * cellSize, 0f, 0f);
            Gizmos.DrawLine(a, a + new Vector3(0f, 0f, d));
        }
        for (int z = 0; z <= depth; z++)
        {
            Vector3 a = origin + new Vector3(0f, 0f, z * cellSize);
            Gizmos.DrawLine(a, a + new Vector3(w, 0f, 0f));
        }

        if (levels == null) return;

        // 층·차단·경사로 표시
        for (int x = 0; x < width; x++)
            for (int z = 0; z < depth; z++)
            {
                Vector2Int c = new Vector2Int(x, z);
                Vector3 p = CellToWorld(c);

                if (ramps[x, z])
                {
                    Gizmos.color = new Color(0.4f, 1f, 0.5f, 0.35f);
                    Gizmos.DrawCube(p, new Vector3(cellSize * 0.7f, 0.05f, cellSize * 0.7f));
                }
                else if (levels[x, z] > 0)
                {
                    Gizmos.color = new Color(0.5f, 0.7f, 1f, 0.18f);
                    Gizmos.DrawCube(p, new Vector3(cellSize * 0.8f, 0.03f, cellSize * 0.8f));
                }
            }
    }
}