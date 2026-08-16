using System.Collections.Generic;
using UnityEngine;

// 건설 모드. B로 진입/종료.
//  방향키 = 고스트 커서 이동 / E = 설치 / X = 철거 / R = 회전(벽은 붙일 변 선택)
//  Q = 다음 부품 / T = 분류 전환(건축 부품 ↔ 설치물) / ESC = 종료
//
// 두 갈래를 다룬다.
//  - 건축 부품(BuildPartDef): 바닥·벽·문·지붕. 자원 비용 없음(팔레트에서 선택)
//  - 설치물(ItemDef.placementPrefab): 제작대 등. 인벤토리에서 1개 소모
public class BuildMode : MonoBehaviour
{
    private enum Category { Parts, Items }

    [Header("키")]
    [SerializeField] private KeyCode toggleKey = KeyCode.B;
    [SerializeField] private KeyCode placeKey = KeyCode.E;
    [SerializeField] private KeyCode removeKey = KeyCode.X;
    [SerializeField] private KeyCode rotateKey = KeyCode.R;
    [SerializeField] private KeyCode cycleKey = KeyCode.Q;
    [SerializeField] private KeyCode categoryKey = KeyCode.T;

    [Header("팔레트")]
    [Tooltip("설치할 수 있는 건축 부품 목록")]
    [SerializeField] private BuildPartDef[] parts;

    [Header("커서")]
    [Tooltip("플레이어로부터 이 칸 수까지만 설치 가능")]
    [SerializeField] private int maxDistanceCells = 8;

    [Header("고스트 색")]
    [SerializeField] private Color okColor = new Color(0.4f, 1f, 0.5f, 0.5f);
    [SerializeField] private Color badColor = new Color(1f, 0.35f, 0.3f, 0.5f);

    [Header("참조 (비우면 씬에서 찾음)")]
    [SerializeField] private Inventory inventory;
    [SerializeField] private WorldGrid grid;
    [SerializeField] private BuildingGrid building;
    [SerializeField] private Transform player;

    public bool IsActive { get; private set; }

    private Category category = Category.Parts;
    private Vector2Int cursor;
    private int rotationStep;         // 0=북, 1=동, 2=남, 3=서
    private bool skipFirstInput;

    private int partIndex;
    private readonly List<ItemDef> placeables = new List<ItemDef>();
    private int placeableIndex;

    private GameObject ghost;
    private Object ghostSource;       // 지금 고스트가 어느 정의로 만들어졌는지

    private GUIStyle labelStyle;

    private void Start()
    {
        if (inventory == null) inventory = FindObjectOfType<Inventory>();
        if (grid == null) grid = WorldGrid.Instance != null ? WorldGrid.Instance : FindObjectOfType<WorldGrid>();
        if (building == null) building = BuildingGrid.Instance != null ? BuildingGrid.Instance : FindObjectOfType<BuildingGrid>();
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

    // ---- 현재 선택 ----
    private BuildPartDef CurrentPart
        => (parts != null && partIndex >= 0 && partIndex < parts.Length) ? parts[partIndex] : null;

    private ItemDef CurrentPlaceable
        => (placeableIndex >= 0 && placeableIndex < placeables.Count) ? placeables[placeableIndex] : null;

    private void RefreshPlaceables()
    {
        ItemDef prev = CurrentPlaceable;
        placeables.Clear();
        if (inventory == null) return;

        for (int i = 0; i < inventory.SlotCount; i++)
        {
            Inventory.Slot s = inventory.Slots[i];
            if (s.IsEmpty || !s.def.IsPlaceable) continue;
            if (!placeables.Contains(s.def)) placeables.Add(s.def);
        }

        int idx = prev != null ? placeables.IndexOf(prev) : -1;
        placeableIndex = idx >= 0 ? idx : 0;
    }

    // ---- 진입/종료 ----
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

        if (Input.GetKeyDown(KeyCode.RightArrow)) MoveCursor(new Vector2Int(1, 0));
        if (Input.GetKeyDown(KeyCode.LeftArrow)) MoveCursor(new Vector2Int(-1, 0));
        if (Input.GetKeyDown(KeyCode.UpArrow)) MoveCursor(new Vector2Int(0, 1));
        if (Input.GetKeyDown(KeyCode.DownArrow)) MoveCursor(new Vector2Int(0, -1));

        if (Input.GetKeyDown(rotateKey)) rotationStep = (rotationStep + 1) % 4;

        if (Input.GetKeyDown(categoryKey)) SwitchCategory();
        if (Input.GetKeyDown(cycleKey)) CycleSelection();

        UpdateGhost();

        if (Input.GetKeyDown(placeKey)) TryPlace();
        if (Input.GetKeyDown(removeKey)) TryRemove();
    }

    private void Enter()
    {
        if (grid == null) { Debug.LogWarning("[건설] WorldGrid가 없음"); return; }

        RefreshPlaceables();

        IsActive = true;
        skipFirstInput = true;
        UIInputLock.Push();

        cursor = player != null ? grid.WorldToCell(player.position) : Vector2Int.zero;
        cursor += new Vector2Int(0, 1);
        rotationStep = 0;

        UpdateGhost();
    }

    private void Exit()
    {
        IsActive = false;
        UIInputLock.Release();
        DestroyGhost();
    }

    private void SwitchCategory()
    {
        category = category == Category.Parts ? Category.Items : Category.Parts;
        if (category == Category.Items) RefreshPlaceables();
        DestroyGhost();
    }

    private void CycleSelection()
    {
        if (category == Category.Parts)
        {
            if (parts == null || parts.Length == 0) return;
            partIndex = (partIndex + 1) % parts.Length;
        }
        else
        {
            if (placeables.Count == 0) return;
            placeableIndex = (placeableIndex + 1) % placeables.Count;
        }
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

    // ---- 배치 위치 계산 ----
    // 벽·문은 커서 칸의 '변'에, 바닥·지붕은 칸 중심에 놓인다.
    private void GetPartTransform(BuildPartDef def, out Vector3 pos, out Quaternion rot)
    {
        // 높이는 층 바닥 + 부품 정의값 (하드코딩 없음)
        float y = grid.HeightAt(cursor) + (def != null ? def.placementHeight : 0f);
        Vector3 center = grid.CellToWorld(cursor);
        center.y = y;

        if (def != null && def.IsEdgePart)
        {
            float half = grid.CellSize * 0.5f;
            Vector3 offset = rotationStep switch
            {
                0 => new Vector3(0f, 0f, half),   // 북
                1 => new Vector3(half, 0f, 0f),   // 동
                2 => new Vector3(0f, 0f, -half),  // 남
                _ => new Vector3(-half, 0f, 0f),  // 서
            };
            pos = center + offset;
            rot = Quaternion.Euler(0f, rotationStep * 90f, 0f);
            return;
        }

        pos = center;
        rot = Quaternion.Euler(0f, rotationStep * 90f, 0f);
    }

    private BuildingGrid.Slot SlotFor(BuildPartDef def)
    {
        if (def == null) return BuildingGrid.Slot.Floor;

        switch (def.kind)
        {
            case BuildPartDef.PartKind.Roof: return BuildingGrid.Slot.Roof;
            case BuildPartDef.PartKind.Wall:
            case BuildPartDef.PartKind.Door:
                BuildingGrid.NormalizeEdge(cursor, rotationStep, out _, out BuildingGrid.Slot slot);
                return slot;
            default: return BuildingGrid.Slot.Floor;
        }
    }

    private Vector2Int CellFor(BuildPartDef def)
    {
        if (def != null && def.IsEdgePart)
        {
            BuildingGrid.NormalizeEdge(cursor, rotationStep, out Vector2Int c, out _);
            return c;
        }
        return cursor;
    }

    // ---- 판정 ----
    private bool CanPlaceHere(out string reason)
    {
        reason = null;

        if (!grid.InBounds(cursor)) { reason = "범위 밖"; return false; }

        if (category == Category.Parts)
        {
            BuildPartDef def = CurrentPart;
            if (def == null) { reason = "부품 없음"; return false; }
            if (def.prefab == null) { reason = "프리팹 미지정"; return false; }
            if (building == null) { reason = "BuildingGrid 없음"; return false; }

            Vector2Int cell = CellFor(def);
            BuildingGrid.Slot slot = SlotFor(def);
            int partLevel = grid.GetLevel(cursor);

            if (!grid.InBounds(cell)) { reason = "범위 밖"; return false; }
            if (!building.CanPlace(cell, partLevel, slot)) { reason = "이미 있음"; return false; }
            return true;
        }

        ItemDef item = CurrentPlaceable;
        if (item == null) { reason = "설치물 없음"; return false; }
        if (inventory == null || !inventory.Has(item, 1)) { reason = "보유 없음"; return false; }

        Vector2Int f = ItemFootprint(item);
        int itemLevel = grid.GetLevel(cursor);

        for (int x = 0; x < f.x; x++)
            for (int z = 0; z < f.y; z++)
            {
                Vector2Int c = new Vector2Int(cursor.x + x, cursor.y + z);
                if (!grid.InBounds(c)) { reason = "범위 밖"; return false; }
                if (grid.GetOccupant(c) != null) { reason = "이미 무언가 있음"; return false; }
                if (grid.GetLevel(c) != itemLevel) { reason = "높이가 다름"; return false; }
                if (grid.IsRamp(c)) { reason = "경사로 위"; return false; }
            }
        return true;
    }

    private Vector2Int ItemFootprint(ItemDef item)
    {
        if (item == null) return Vector2Int.one;

        GridOccupant occ = item.placementPrefab != null
            ? item.placementPrefab.GetComponent<GridOccupant>() : null;
        Vector2Int f = occ != null ? occ.Footprint : item.placementFootprint;

        if (rotationStep % 2 == 1) f = new Vector2Int(f.y, f.x);
        return new Vector2Int(Mathf.Max(1, f.x), Mathf.Max(1, f.y));
    }

    // ---- 고스트 ----
    private void UpdateGhost()
    {
        Object source = category == Category.Parts ? (Object)CurrentPart : CurrentPlaceable;
        GameObject prefab = category == Category.Parts
            ? (CurrentPart != null ? CurrentPart.prefab : null)
            : (CurrentPlaceable != null ? CurrentPlaceable.placementPrefab : null);

        if (prefab == null) { DestroyGhost(); return; }

        if (ghost == null || ghostSource != source)
        {
            DestroyGhost();
            ghostSource = source;
            ghost = Instantiate(prefab);
            ghost.name = "BuildGhost";
            StripGhost(ghost);
        }

        if (category == Category.Parts)
        {
            GetPartTransform(CurrentPart, out Vector3 pos, out Quaternion rot);
            ghost.transform.SetPositionAndRotation(pos, rot);
        }
        else
        {
            Vector2Int f = ItemFootprint(CurrentPlaceable);
            ghost.transform.SetPositionAndRotation(
                grid.CellToWorldCenter(cursor, f),
                Quaternion.Euler(0f, rotationStep * 90f, 0f));
        }

        Tint(ghost, CanPlaceHere(out _) ? okColor : badColor);
    }

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
        ghostSource = null;
    }

    // ---- 설치·철거 ----
    private void TryPlace()
    {
        if (!CanPlaceHere(out string reason))
        {
            Debug.Log($"[건설] 설치 불가 — {reason}");
            return;
        }

        if (category == Category.Parts)
        {
            BuildPartDef def = CurrentPart;
            GetPartTransform(def, out Vector3 pos, out Quaternion rot);

            GameObject go = Instantiate(def.prefab, pos, rot);
            go.name = def.displayName;

            if (!building.Place(CellFor(def), grid.GetLevel(cursor), SlotFor(def), def, go))
            {
                Destroy(go);
                Debug.Log("[건설] 설치 실패 — 이미 있음");
            }
            return;
        }

        ItemDef item = CurrentPlaceable;
        if (!inventory.TrySpend(item, 1)) { Debug.Log("[건설] 아이템 부족"); return; }

        Vector2Int f = ItemFootprint(item);
        Instantiate(item.placementPrefab,
                    grid.CellToWorldCenter(cursor, f),
                    Quaternion.Euler(0f, rotationStep * 90f, 0f));

        if (!inventory.Has(item, 1))
        {
            RefreshPlaceables();
            DestroyGhost();
        }
    }

    // 현재 분류·회전 기준으로 커서 위치의 부품을 제거
    private void TryRemove()
    {
        if (category != Category.Parts || building == null) return;

        BuildPartDef def = CurrentPart;
        if (def == null) return;

        Vector2Int cell = CellFor(def);
        BuildingGrid.Slot slot = SlotFor(def);
        int level = grid.GetLevel(cursor);

        Debug.Log(building.Remove(cell, level, slot)
            ? $"[건설] 철거: {cell} L{level} / {slot}"
            : $"[건설] 철거할 것 없음: {cell} L{level} / {slot}");
    }

    // ---- 안내 ----
    private void OnGUI()
    {
        if (!IsActive) return;

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.box) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
            labelStyle.normal.textColor = Color.white;
        }

        string name;
        string count = "";

        if (category == Category.Parts)
        {
            BuildPartDef def = CurrentPart;
            name = def != null ? $"{def.displayName} ({def.kind})" : "부품 없음";
            if (parts != null && parts.Length > 1) count = $"  ({partIndex + 1}/{parts.Length})";
        }
        else
        {
            ItemDef item = CurrentPlaceable;
            int have = (item != null && inventory != null) ? inventory.Get(item) : 0;
            name = item != null ? $"{item.displayName} x{have}" : "설치물 없음";
            if (placeables.Count > 1) count = $"  ({placeableIndex + 1}/{placeables.Count})";
        }

        CanPlaceHere(out string reason);
        string dir = rotationStep switch { 0 => "북", 1 => "동", 2 => "남", _ => "서" };
        int level = grid != null ? grid.GetLevel(cursor) : 0;

        string text =
            $"건설 [{(category == Category.Parts ? "건축 부품" : "설치물")}]  {name}{count}   " +
            $"칸 ({cursor.x},{cursor.y}) L{level}   방향 {dir}" +
            (reason != null ? $"   ({reason})" : "   설치 가능") +
            "\n방향키 이동 · E 설치 · X 철거 · R 회전 · Q 다음 · T 분류 전환 · ESC 종료";

        GUI.Box(new Rect((Screen.width - 640f) * 0.5f, 20f, 640f, 48f), text, labelStyle);
    }
}