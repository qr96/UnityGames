using System.Collections;
using UnityEngine;

// 맵 로드가 끝나면 플레이어를 화로 옆으로 옮긴다.
// 로드 순서에 따라 이벤트를 놓칠 수 있어, 시작 후 한 프레임 뒤에도 한 번 더 시도한다.
public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private Transform player;   // 비우면 PlayerMovement로 찾음
    [SerializeField] private MapLoader loader;   // 비우면 씬에서 찾음
    [SerializeField] private WorldGrid grid;     // 비우면 씬에서 찾음

    [Header("배치")]
    [Tooltip("화로에서 떨어질 거리(칸)")]
    [SerializeField] private float offsetCells = 2f;
    [Tooltip("화로 기준 어느 방향에 세울지")]
    [SerializeField] private Vector2 offsetDirection = new Vector2(0f, -1f);

    [Header("디버그")]
    [SerializeField] private bool verboseLog = true;

    private bool spawned;

    private void Awake()
    {
        if (loader == null) loader = FindObjectOfType<MapLoader>();
        if (loader != null) loader.OnLoaded += OnMapLoaded;
        else if (verboseLog) Debug.Log("[스폰] MapLoader 없음 — 시작 시 바로 시도");
    }

    private void OnDestroy()
    {
        if (loader != null) loader.OnLoaded -= OnMapLoaded;
    }

    private void Start() => StartCoroutine(LateAttempt());

    // 맵 로드 이벤트를 놓쳤을 경우를 위한 재시도
    private IEnumerator LateAttempt()
    {
        yield return null; // 모든 Start가 끝난 뒤
        if (!spawned) Spawn();
    }

    private void OnMapLoaded() => Spawn();

    public void Spawn()
    {
        if (player == null)
        {
            PlayerMovement pm = FindObjectOfType<PlayerMovement>();
            if (pm != null) player = pm.transform;
        }
        if (player == null)
        {
            Debug.LogWarning("[스폰] 플레이어를 찾지 못함 (PlayerMovement가 붙은 오브젝트 필요)");
            return;
        }

        if (grid == null) grid = WorldGrid.Instance != null ? WorldGrid.Instance : FindObjectOfType<WorldGrid>();

        if (Hearth.All.Count == 0)
        {
            Debug.LogWarning("[스폰] 씬에 화로가 없음 — 맵 JSON의 hearth 배치와 MapCatalog 등록 확인");
            return;
        }

        Transform hearth = Hearth.All[0].transform;

        Vector2 dir = offsetDirection.sqrMagnitude > 0.0001f
            ? offsetDirection.normalized
            : new Vector2(0f, -1f);

        float step = grid != null ? grid.CellSize : 1f;
        Vector3 pos = hearth.position + new Vector3(dir.x, 0f, dir.y) * (offsetCells * step);

        if (grid != null)
        {
            Vector2Int cell = grid.WorldToCell(pos);
            if (!grid.CanStand(cell))
            {
                if (verboseLog) Debug.Log($"[스폰] {cell} 가 막혀 화로 자리로 되돌림");
                pos = hearth.position;
            }
            pos.y = grid.SampleHeight(pos);
        }

        Vector3 before = player.position;
        Teleport(pos);
        spawned = true;

        if (verboseLog)
        {
            Debug.Log($"[스폰] 화로({hearth.name}) 기준 배치 — {before} → {player.position} " +
                      $"(화로 위치 {hearth.position}, 화로 수 {Hearth.All.Count})", player.gameObject);
            Debug.Log($"[스폰] 대상 오브젝트: {FullPath(player)}  " +
                      $"(부모 {(player.parent != null ? player.parent.name : "없음")}, " +
                      $"localPosition {player.localPosition})", player.gameObject);

            StartCoroutine(VerifyNextFrame(player.position));
        }
    }

    // 스폰 직후 위치가 유지되는지 확인 — 되돌려지면 다른 코드가 개입하고 있다
    private IEnumerator VerifyNextFrame(Vector3 expected)
    {
        yield return null;

        Vector3 now = player.position;
        if ((now - expected).sqrMagnitude > 0.25f)
        {
            Debug.LogWarning($"[스폰] 한 프레임 뒤 위치가 바뀜: {expected} → {now} " +
                             "— 다른 스크립트나 상위 오브젝트가 위치를 되돌리고 있음", player.gameObject);
        }
        else if (verboseLog)
        {
            Debug.Log($"[스폰] 위치 유지 확인: {now}", player.gameObject);
        }

        // 씬에 플레이어 후보가 여럿이면 알림 (엉뚱한 오브젝트를 보고 있을 수 있음)
        PlayerMovement[] all = FindObjectsOfType<PlayerMovement>();
        if (all.Length > 1)
        {
            string names = "";
            for (int i = 0; i < all.Length; i++) names += FullPath(all[i].transform) + "  ";
            Debug.LogWarning($"[스폰] PlayerMovement가 {all.Length}개 있음 — {names}");
        }
    }

    private static string FullPath(Transform t)
    {
        string path = t.name;
        Transform p = t.parent;
        while (p != null) { path = p.name + "/" + path; p = p.parent; }
        return path;
    }

    private void Teleport(Vector3 pos)
    {
        CharacterController cc = player.GetComponent<CharacterController>();

        // 캡슐 밑면을 지면에 맞춤
        if (cc != null) pos.y += cc.height * 0.5f - cc.center.y;

        if (cc != null) cc.enabled = false;
        player.position = pos;
        if (cc != null) cc.enabled = true;
    }
}