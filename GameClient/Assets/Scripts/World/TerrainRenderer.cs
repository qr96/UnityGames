using System.Collections.Generic;
using UnityEngine;

// 지형 렌더러. 격자를 청크 단위로 나눠 메시를 굽고, 바뀐 청크만 다시 굽는다.
// 상판/절벽 머티리얼을 따로 지정하며, 아트가 들어와도 이 구조는 유지된다.
public class TerrainRenderer : MonoBehaviour
{
    [SerializeField] private WorldGrid grid; // 비우면 씬에서 찾음

    [Header("청크")]
    [Tooltip("청크 한 변의 칸 수")]
    [SerializeField] private int chunkSize = 16;

    [Header("머티리얼")]
    [Tooltip("층 상판(눈·땅)")]
    [SerializeField] private Material topMaterial;
    [Tooltip("절벽 옆면(암벽)")]
    [SerializeField] private Material cliffMaterial;

    [Header("충돌")]
    [Tooltip("통행은 격자 판정이라 보통 불필요. 물리 판정이 필요할 때만 켜기")]
    [SerializeField] private bool generateColliders = false;

    private readonly Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();
    private readonly HashSet<Vector2Int> dirty = new HashSet<Vector2Int>();
    private bool rebuildAll;

    private class Chunk
    {
        public GameObject go;
        public MeshFilter filter;
        public MeshCollider collider;
        public Mesh mesh;
    }

    private void Awake()
    {
        if (grid == null) grid = WorldGrid.Instance != null ? WorldGrid.Instance : FindObjectOfType<WorldGrid>();
        if (grid == null) { Debug.LogWarning("[지형] WorldGrid가 없음"); return; }

        // 부모 트랜스폼이 기본값이 아니면 지형이 통째로 어긋난다
        if (transform.rotation != Quaternion.identity || transform.localScale != Vector3.one)
            Debug.LogWarning($"[지형] {name}의 회전/스케일이 기본값이 아님 — 지형이 어긋날 수 있음 " +
                             $"(rotation {transform.eulerAngles}, scale {transform.localScale})");

        grid.OnCellChanged += OnCellChanged;
        grid.OnGridReloaded += OnGridReloaded;
    }

    private void OnDestroy()
    {
        if (grid == null) return;
        grid.OnCellChanged -= OnCellChanged;
        grid.OnGridReloaded -= OnGridReloaded;
    }

    private void Start()
    {
        // 맵 로더가 먼저 돌았으면 OnGridReloaded로 이미 예약됨. 아니면 여기서 전체 생성.
        rebuildAll = true;
    }

    private void OnGridReloaded() => rebuildAll = true;

    private void OnCellChanged(Vector2Int cell)
    {
        // 경계 칸은 이웃 청크의 절벽면에도 영향 → 주변 청크까지 갱신
        for (int dx = -1; dx <= 1; dx++)
            for (int dz = -1; dz <= 1; dz++)
            {
                Vector2Int c = new Vector2Int(cell.x + dx, cell.y + dz);
                if (!grid.InBounds(c)) continue;
                dirty.Add(ChunkOf(c));
            }
    }

    private void LateUpdate()
    {
        if (grid == null) return;

        if (rebuildAll)
        {
            rebuildAll = false;
            dirty.Clear();
            BuildAll();
            return;
        }

        if (dirty.Count == 0) return;

        foreach (Vector2Int key in dirty) BuildChunk(key);
        dirty.Clear();
    }

    private Vector2Int ChunkOf(Vector2Int cell)
    {
        int cs = Mathf.Max(1, chunkSize);
        return new Vector2Int(Mathf.FloorToInt(cell.x / (float)cs), Mathf.FloorToInt(cell.y / (float)cs));
    }

    private void BuildAll()
    {
        int cs = Mathf.Max(1, chunkSize);
        int cx = Mathf.CeilToInt(grid.Width / (float)cs);
        int cz = Mathf.CeilToInt(grid.Depth / (float)cs);

        // 범위 밖으로 남은 청크 정리
        var stale = new List<Vector2Int>();
        foreach (var kv in chunks)
            if (kv.Key.x >= cx || kv.Key.y >= cz) stale.Add(kv.Key);
        foreach (var key in stale) { Destroy(chunks[key].go); chunks.Remove(key); }

        for (int x = 0; x < cx; x++)
            for (int z = 0; z < cz; z++)
                BuildChunk(new Vector2Int(x, z));

        Debug.Log($"[지형] 청크 {cx}x{cz} 생성 (청크당 {cs}x{cs}칸)");
    }

    private void BuildChunk(Vector2Int key)
    {
        int cs = Mathf.Max(1, chunkSize);
        int minX = key.x * cs;
        int minZ = key.y * cs;
        int maxX = Mathf.Min(minX + cs - 1, grid.Width - 1);
        int maxZ = Mathf.Min(minZ + cs - 1, grid.Depth - 1);
        if (minX > maxX || minZ > maxZ) return;

        Chunk chunk = GetOrCreate(key, minX, minZ);
        TerrainMeshBuilder.Build(grid, chunk.mesh, minX, minZ, maxX, maxZ, chunk.go.transform.position);

        chunk.filter.sharedMesh = chunk.mesh;

        if (generateColliders)
        {
            if (chunk.collider == null) chunk.collider = chunk.go.AddComponent<MeshCollider>();
            chunk.collider.sharedMesh = null;
            chunk.collider.sharedMesh = chunk.mesh;
        }
        else if (chunk.collider != null)
        {
            Destroy(chunk.collider);
            chunk.collider = null;
        }
    }

    private Chunk GetOrCreate(Vector2Int key, int minX, int minZ)
    {
        if (chunks.TryGetValue(key, out Chunk existing)) return existing;

        var go = new GameObject($"Chunk_{key.x}_{key.y}");
        go.transform.SetParent(transform, false);
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        go.transform.position = grid.Origin + new Vector3(minX * grid.CellSize, 0f, minZ * grid.CellSize);

        var chunk = new Chunk
        {
            go = go,
            filter = go.AddComponent<MeshFilter>(),
            mesh = new Mesh { name = $"Terrain_{key.x}_{key.y}" },
        };

        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = new[] { topMaterial, cliffMaterial };

        chunks[key] = chunk;
        return chunk;
    }
}