using UnityEngine;

// 격자 인벤토리 UI. 키보드 전용:
//  Tab 열기/닫기, 방향키 커서 이동,
//  숫자키 1~5 = 선택한 장비를 그 핫바 슬롯에 배정, F = 선택한 음식을 퀵푸드로 지정,
//  Q/ESC 닫기. 임시 그래픽(OnGUI).
public class InventoryGridUI : MonoBehaviour
{
    [SerializeField] private Inventory inventory;   // 비우면 씬에서 찾음
    [SerializeField] private Hotbar hotbar;        // 비우면 씬에서 찾음
    [SerializeField] private QuickFood quickFood;  // 비우면 씬에서 찾음
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
        if (hotbar == null) hotbar = FindObjectOfType<Hotbar>();
        if (quickFood == null) quickFood = FindObjectOfType<QuickFood>();
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

        Inventory.Slot slot = inventory.Slots[cursor];

        // 숫자키: 선택한 장비를 핫바에 배정
        for (int i = 0; i < Hotbar.SlotCount; i++)
        {
            if (!Input.GetKeyDown(KeyCode.Alpha1 + i)) continue;

            if (slot.IsEmpty) { Debug.Log("[격자] 빈 칸 — 배정할 것 없음"); break; }
            if (!slot.def.IsEquipment)
            {
                Debug.Log("[격자] 핫바에는 장비만 올릴 수 있음 (음식은 F, 설치물은 건설 모드)");
                break;
            }
            if (hotbar != null) hotbar.Assign(i, slot.def);
            break;
        }

        // F: 선택한 음식을 퀵푸드로 지정
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (slot.IsEmpty) Debug.Log("[격자] 빈 칸 — 지정할 것 없음");
            else if (quickFood == null) Debug.LogWarning("[격자] QuickFood가 씬에 없음");
            else if (!quickFood.Assign(slot.def)) Debug.Log("[격자] 음식만 F 슬롯에 지정할 수 있음");
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

        int used = 0;
        for (int i = 0; i < inventory.SlotCount; i++)
            if (!inventory.Slots[i].IsEmpty) used++;

        GUI.Label(new Rect(px + 16f, py + 8f, panelW - 32f, 22f),
            $"칸 {used}/{inventory.SlotCount}   골드 {inventory.Gold}", headStyle);
        GUI.Label(new Rect(px + 16f, py + panelH - 26f, panelW - 32f, 22f),
            "방향키 이동 · 숫자키 1~5 핫바 배정 · F 퀵푸드 지정 · Q/ESC 닫기", headStyle);

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