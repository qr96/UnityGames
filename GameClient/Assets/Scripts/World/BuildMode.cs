using UnityEngine;

// 건설 모드. B로 진입/종료, 방향키로 고스트 커서 이동, E 설치, R 회전, Q/ESC 취소.
// 놓을 물건은 이 모드 안에서 Q로 순환해 고른다(인벤토리에 있는 설치물 목록). 핫바와 무관.
// 설치 가능 판정: 격자 범위 안 · 점유 없음 · 발판 전체가 같은 층.
public class BuildMode : MonoBehaviour
{
    [Header("키")]
    [SerializeField] private KeyCode toggleKey = KeyCode.B;
    [SerializeField] private KeyCode placeKey = KeyCode.E;
    [SerializeField] private KeyCode rotateKey = KeyCode.R;
    [Tooltip("놓을 설치물 순환")]
    [SerializeField] private KeyCode cycleKey = KeyCode.Q;

    [Header("커서")]
    [Tooltip("플레이어로부터 이 칸 수까지만 설치 가능")]
    [SerializeField] private int maxDistanceCells = 6;

    [Header("고스트 색")]
    [SerializeField] private Color okColor = new Color(0.4f, 1f, 0.5f, 0.5f);
    [SerializeField] private Color badColor = new Color(1f, 0.35f, 0.3f, 0.5f);

    [SerializeField] private Inventory inventory;   // 비우면 씬에서 찾음
    [SerializeField] private WorldGrid grid;        // 비우면 씬에서 찾음
    [SerializeField] private Transform player;      // 비우면 PlayerMovement로 찾음

    public bool IsActive { get; private set; }

    private Vector2Int cursor;
    private int rotationStep;          // 0~3 (90도 단위)
    private ItemDef ghostDef;
    private GameObject ghost;
    private bool skipFirstInput;

    private GUIStyle labelStyle;

    private void Start()
    {
        if (inventory == null) inventory = FindObjectOfType<Inventory>();
        if (grid == null) grid = WorldGrid.Instance != null ? WorldGrid.Instance : FindObjectOfType<WorldGrid>();
        if (player == null)
        {
            PlayerMovement pm = FindObjectOfType<PlayerMovement>();
            if (pm != null) player = pm.transform;
        }
    }

    private void OnDisable()
    {
        if (IsActive) Exit();
    }

    private readonly System.Collections.Generic.List<ItemDef> placeables =
        new System.Collections.Generic.List<ItemDef>();
    private int placeableIndex;

    // 인벤토리에 있는 설치물 목록 갱신
    private void RefreshPlaceables()
    {
        ItemDef prev = SelectedPlaceable;
        placeables.Clear();
        if (inventory == null) return;

        for (int i = 0; i < inventory.SlotCount; i++)
        {
            Inventory.Slot s = inventory.Slots[i];
            if (s.IsEmpty || !s.def.IsPlaceable) continue;
            if (!placeables.Contains(s.def)) placeables.Add(s.def);
        }

        // 이전 선택 유지
        int idx = prev != null ? placeables.IndexOf(prev) : -1;
        placeableIndex = idx >= 0 ? idx : 0;
    }

    // 지금 놓으려는 설치물
    private ItemDef SelectedPlaceable
        => (placeableIndex >= 0 && placeableIndex < placeables.Count) ? placeables[placeableIndex] : null;

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (IsActive) Exit();
            else Enter();
        }

        if (!IsActive) return;

        if (skipFirstInput) { skipFirstInput = false; return; }

        if (Input.GetKeyDown(KeyCode.Escape)) { Exit(); return; }

        // 커서 이동
        if (Input.GetKeyDown(KeyCode.RightArrow)) MoveCursor(new Vector2Int(1, 0));
        if (Input.GetKeyDown(KeyCode.LeftArrow)) MoveCursor(new Vector2Int(-1, 0));
        if (Input.GetKeyDown(KeyCode.UpArrow)) MoveCursor(new Vector2Int(0, 1));
        if (Input.GetKeyDown(KeyCode.DownArrow)) MoveCursor(new Vector2Int(0, -1));

        if (Input.GetKeyDown(rotateKey)) rotationStep = (rotationStep + 1) % 4;

        if (Input.GetKeyDown(cycleKey) && placeables.Count > 1)
        {
            placeableIndex = (placeableIndex + 1) % placeables.Count;
            DestroyGhost();
        }

        UpdateGhost();

        if (Input.GetKeyDown(placeKey)) TryPlace();
    }

    private void Enter()
    {
        if (grid == null) { Debug.LogWarning("[건설] WorldGrid가 없음"); return; }

        RefreshPlaceables();
        if (SelectedPlaceable == null)
        {
            Debug.Log("[건설] 설치할 수 있는 아이템이 없음 (제작 후 다시 시도)");
            return;
        }

        IsActive = true;
        skipFirstInput = true;
        UIInputLock.Push();

        cursor = player != null ? grid.WorldToCell(player.position) : Vector2Int.zero;
        cursor += new Vector2Int(0, 1); // 플레이어 앞 칸에서 시작
        rotationStep = 0;

        UpdateGhost();
    }

    private void Exit()
    {
        IsActive = false;
        UIInputLock.Release();
        DestroyGhost();
    }

    private void MoveCursor(Vector2Int delta)
    {
        Vector2Int next = cursor + delta;
        if (!grid.InBounds(next)) return;

        if (player != null)
        {
            Vector2Int pc = grid.WorldToCell(player.position);
            if (Mathf.Abs(next.x - pc.x) > maxDistanceCells ||
                Mathf.Abs(next.y - pc.y) > maxDistanceCells) return;
        }

        cursor = next;
    }

    private Vector2Int Footprint(ItemDef def)
    {
        if (def == null) return Vector2Int.one;

        GridOccupant occ = def.placementPrefab != null
            ? def.placementPrefab.GetComponent<GridOccupant>() : null;
        Vector2Int f = occ != null ? occ.Footprint : def.placementFootprint;

        // 90도·270도 회전 시 가로세로 교환
        if (rotationStep % 2 == 1) f = new Vector2Int(f.y, f.x);
        return new Vector2Int(Mathf.Max(1, f.x), Mathf.Max(1, f.y));
    }

    private bool CanPlaceHere(ItemDef def, out string reason)
    {
        reason = null;
        if (def == null) { reason = "설치물 없음"; return false; }
        if (grid == null) { reason = "격자 없음"; return false; }

        Vector2Int f = Footprint(def);
        int level = grid.GetLevel(cursor);

        for (int x = 0; x < f.x; x++)
            for (int z = 0; z < f.y; z++)
            {
                Vector2Int c = new Vector2Int(cursor.x + x, cursor.y + z);
                if (!grid.InBounds(c)) { reason = "범위 밖"; return false; }
                if (grid.GetOccupant(c) != null) { reason = "이미 무언가 있음"; return false; }
                if (grid.GetLevel(c) != level) { reason = "높이가 다름"; return false; }
                if (grid.IsRamp(c)) { reason = "경사로 위"; return false; }
            }
        return true;
    }

    private void UpdateGhost()
    {
        ItemDef def = SelectedPlaceable;

        if (def == null) { DestroyGhost(); return; }

        if (ghost == null || ghostDef != def)
        {
            DestroyGhost();
            ghostDef = def;
            ghost = Instantiate(def.placementPrefab);
            ghost.name = "BuildGhost";
            StripGhost(ghost);
        }

        Vector2Int f = Footprint(def);
        ghost.transform.position = grid.CellToWorldCenter(cursor, f);
        ghost.transform.rotation = Quaternion.Euler(0f, rotationStep * 90f, 0f);

        bool ok = CanPlaceHere(def, out _);
        Tint(ghost, ok ? okColor : badColor);
    }

    // 고스트는 로직이 돌면 안 되므로 스크립트·콜라이더를 끈다
    private static void StripGhost(GameObject go)
    {
        MonoBehaviour[] scripts = go.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < scripts.Length; i++) scripts[i].enabled = false;

        Collider[] colliders = go.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = false;
    }

    private static void Tint(GameObject go, Color c)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rs.Length; i++)
        {
            Material m = rs[i].material;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }
    }

    private void DestroyGhost()
    {
        if (ghost != null) Destroy(ghost);
        ghost = null;
        ghostDef = null;
    }

    private void TryPlace()
    {
        ItemDef def = SelectedPlaceable;
        if (!CanPlaceHere(def, out string reason))
        {
            Debug.Log($"[건설] 설치 불가 — {reason}");
            return;
        }

        if (!inventory.TrySpend(def, 1))
        {
            Debug.Log("[건설] 아이템 부족");
            return;
        }

        Vector2Int f = Footprint(def);
        Vector3 pos = grid.CellToWorldCenter(cursor, f);
        Instantiate(def.placementPrefab, pos, Quaternion.Euler(0f, rotationStep * 90f, 0f));

        // 더 놓을 게 없으면 목록 갱신 후 종료 판단
        if (!inventory.Has(def, 1))
        {
            RefreshPlaceables();
            DestroyGhost();
            if (SelectedPlaceable == null) { Exit(); return; }
        }
        UpdateGhost();
    }

    private void OnGUI()
    {
        if (!IsActive) return;

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.box) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
            labelStyle.normal.textColor = Color.white;
        }

        ItemDef def = SelectedPlaceable;
        string name = def != null ? def.displayName : "설치물 없음";
        int have = (def != null && inventory != null) ? inventory.Get(def) : 0;
        CanPlaceHere(def, out string reason);

        string text = $"건설 — {name} x{have}" +
                      (placeables.Count > 1 ? $"  ({placeableIndex + 1}/{placeables.Count})" : "") +
                      $"   칸 ({cursor.x},{cursor.y})   회전 {rotationStep * 90}°" +
                      (reason != null ? $"   ({reason})" : "   설치 가능") +
                      "\n방향키 이동 · E 설치 · R 회전 · Q 다음 설치물 · ESC 취소";

        GUI.Box(new Rect((Screen.width - 520f) * 0.5f, 20f, 520f, 48f), text, labelStyle);
    }
}