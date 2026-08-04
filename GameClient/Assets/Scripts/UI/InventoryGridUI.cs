using UnityEngine;

// 격자 인벤토리 UI. 키보드 전용:
//  Tab 열기/닫기, 방향키 커서 이동, 숫자키 1~5 = 선택한 칸의 아이템을 그 퀵슬롯에 배정,
//  Q/ESC 닫기. 임시 그래픽(OnGUI).
public class InventoryGridUI : MonoBehaviour
{
    [SerializeField] private Inventory inventory;   // 비우면 씬에서 찾음
    [SerializeField] private QuickSlotBar quickBar; // 비우면 씬에서 찾음
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;
    [SerializeField] private int columns = 5;

    public bool IsOpen { get; private set; }

    private int cursor;

    // 열림 상태 변경 시 캐릭터 입력 잠금(UIInputLock)을 함께 갱신
    private void SetOpen(bool open)
    {
        if (IsOpen == open) return;
        IsOpen = open;
        if (open) UIInputLock.Push();
        else UIInputLock.Release();
    }

    private void OnDisable()
    {
        if (IsOpen) { IsOpen = false; UIInputLock.Release(); }
    }

    private void Start()
    {
        if (inventory == null) inventory = FindObjectOfType<Inventory>();
        if (quickBar == null) quickBar = FindObjectOfType<QuickSlotBar>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey)) SetOpen(!IsOpen);
        if (!IsOpen) return;

        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Escape))
        {
            SetOpen(false);
            return;
        }

        if (inventory == null) return;
        int n = inventory.SlotCount;
        if (n <= 0) return;

        if (Input.GetKeyDown(KeyCode.RightArrow)) cursor = (cursor + 1) % n;
        if (Input.GetKeyDown(KeyCode.LeftArrow)) cursor = (cursor - 1 + n) % n;
        if (Input.GetKeyDown(KeyCode.DownArrow)) cursor = Mathf.Min(n - 1, cursor + columns);
        if (Input.GetKeyDown(KeyCode.UpArrow)) cursor = Mathf.Max(0, cursor - columns);

        // 숫자키: 선택한 칸의 아이템을 퀵슬롯에 배정 (도구·음식만)
        for (int i = 0; i < QuickSlotBar.SlotCount; i++)
        {
            if (!Input.GetKeyDown(KeyCode.Alpha1 + i)) continue;

            Inventory.Slot s = inventory.Slots[cursor];
            if (s.IsEmpty) { Debug.Log("[격자] 빈 칸 — 배정할 것 없음"); break; }
            if (!s.def.IsTool && !s.def.IsFood && !s.def.IsPlaceable)
            {
                Debug.Log("[격자] 퀵슬롯에는 도구·음식·설치물만 배정");
                break;
            }
            if (quickBar != null) quickBar.Assign(i, s.def);
            break;
        }
    }

    private GUIStyle cellStyle;
    private GUIStyle headStyle;

    private void OnGUI()
    {
        if (!IsOpen || inventory == null) return;

        if (cellStyle == null)
        {
            cellStyle = new GUIStyle(GUI.skin.box) { fontSize = 12, alignment = TextAnchor.MiddleCenter };
            headStyle = new GUIStyle(GUI.skin.label) { fontSize = 15 };
            headStyle.normal.textColor = Color.white;
        }

        const float cell = 74f, gap = 5f;
        int n = inventory.SlotCount;
        int rows = Mathf.CeilToInt(n / (float)columns);

        float gridW = columns * cell + (columns - 1) * gap;
        float gridH = rows * cell + (rows - 1) * gap;
        float panelW = gridW + 32f;
        float panelH = gridH + 78f;
        float px = (Screen.width - panelW) * 0.5f;
        float py = (Screen.height - panelH) * 0.5f;

        GUI.Box(new Rect(px, py, panelW, panelH), GUIContent.none);

        string over = inventory.IsOverweight
            ? $"  — 초과 (속도 x{inventory.SpeedMultiplier:0.00})" : "";
        GUI.Label(new Rect(px + 16f, py + 8f, panelW - 32f, 22f),
            $"무게 {inventory.CurrentWeight:0.#}/{inventory.WeightLimit:0.#}{over}   골드 {inventory.Gold}", headStyle);
        GUI.Label(new Rect(px + 16f, py + panelH - 26f, panelW - 32f, 22f),
            "방향키 이동 · 숫자키 1~5 퀵슬롯 배정 · Q/ESC 닫기", headStyle);

        float gx = px + 16f;
        float gy = py + 34f;

        for (int i = 0; i < n; i++)
        {
            int col = i % columns;
            int row = i / columns;
            Rect r = new Rect(gx + col * (cell + gap), gy + row * (cell + gap), cell, cell);

            Inventory.Slot s = inventory.Slots[i];
            string text = s.IsEmpty ? "-" : $"{s.def.displayName}\n{s.count}";

            Color prev = GUI.color;
            if (i == cursor) GUI.color = new Color(1f, 0.95f, 0.6f);
            GUI.Box(r, text, cellStyle);
            GUI.color = prev;
        }
    }
}