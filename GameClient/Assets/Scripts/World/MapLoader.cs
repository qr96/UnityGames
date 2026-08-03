using UnityEngine;

// 맵 파일(JSON)을 읽어 씬에 배치한다. 생성은 외부에서 해도 되고, 이 로더는 형식만 본다.
// 배치물은 격자 칸 중심에 놓이며, GridOccupant가 붙어 있으면 점유가 자동 등록된다.
public class MapLoader : MonoBehaviour
{
    // 로드가 끝난 뒤 발생 — 확인용 시각화 등이 구독
    public event System.Action OnLoaded;

    [Header("입력")]
    [Tooltip("맵 JSON 파일(TextAsset). 프로젝트에 넣고 여기 연결")]
    [SerializeField] private TextAsset mapJson;
    [Tooltip("비우면 mapJson 사용. 값이 있으면 이 경로의 파일을 읽음(절대 경로 또는 Application 기준)")]
    [SerializeField] private string externalFilePath = "";

    [Header("대응표")]
    [SerializeField] private MapCatalog catalog;

    [Header("배치")]
    [SerializeField] private WorldGrid grid;           // 비우면 씬에서 찾음
    [Tooltip("생성물을 담을 부모. 비우면 이 오브젝트")]
    [SerializeField] private Transform container;
    [SerializeField] private bool loadOnStart = true;

    private void Start()
    {
        if (grid == null) grid = WorldGrid.Instance != null ? WorldGrid.Instance : FindObjectOfType<WorldGrid>();
        if (container == null) container = transform;
        if (loadOnStart) Load();
    }

    public void Load()
    {
        string json = ReadJson();
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("[맵] 읽을 JSON이 없음 (Map Json 또는 External File Path 확인)");
            return;
        }

        MapData data = null;
        try { data = JsonUtility.FromJson<MapData>(json); }
        catch (System.Exception e) { Debug.LogError($"[맵] JSON 해석 실패: {e.Message}"); return; }

        if (data == null || data.placements == null)
        {
            Debug.LogWarning("[맵] placements가 비어 있음");
            return;
        }

        if (grid == null)
        {
            Debug.LogWarning("[맵] WorldGrid가 없어 배치할 수 없음");
            return;
        }

        // 지형(층·차단·경사로)과 규격을 격자에 적용 — 배치보다 먼저
        grid.ApplyMapData(data);

        int placed = 0, failed = 0;

        for (int i = 0; i < data.placements.Length; i++)
        {
            MapPlacement p = data.placements[i];
            GameObject prefab = catalog != null ? catalog.Find(p.id) : null;
            if (prefab == null)
            {
                Debug.LogWarning($"[맵] id '{p.id}' 를 MapCatalog에서 찾지 못함 — 건너뜀");
                failed++;
                continue;
            }

            Vector2Int cell = new Vector2Int(p.x, p.z);
            if (!grid.InBounds(cell))
            {
                Debug.LogWarning($"[맵] '{p.id}' 좌표가 범위 밖: ({p.x},{p.z})");
                failed++;
                continue;
            }

            // footprint는 프리팹의 GridOccupant에서 읽음(없으면 1x1)
            GridOccupant occ = prefab.GetComponent<GridOccupant>();
            Vector2Int footprint = occ != null ? occ.Footprint : Vector2Int.one;

            Vector3 pos = grid.CellToWorldCenter(cell, footprint);
            Quaternion rot = Quaternion.Euler(0f, p.rotationY, 0f);

            Instantiate(prefab, pos, rot, container);
            placed++;
        }

        Debug.Log($"[맵] 배치 완료 — 성공 {placed} / 실패 {failed}");
        OnLoaded?.Invoke();
    }

    private string ReadJson()
    {
        if (!string.IsNullOrEmpty(externalFilePath))
        {
            try { return System.IO.File.ReadAllText(externalFilePath); }
            catch (System.Exception e)
            {
                Debug.LogError($"[맵] 파일 읽기 실패: {e.Message}");
                return null;
            }
        }
        return mapJson != null ? mapJson.text : null;
    }
}