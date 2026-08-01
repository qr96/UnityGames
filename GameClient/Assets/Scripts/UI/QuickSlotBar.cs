using System;
using UnityEngine;

// 퀵 슬롯 5칸. 숫자키 1~5로 선택, 사용 키(기본 R)로 사용.
//  - 음식: 1개 소모 → PlayerStats.Eat(hungerRestore)
//  - 도구: OnToolUsed 이벤트 발생(도끼질 미니게임 마디에서 연결). 소모 없음.
// 배정은 InventoryGridUI에서 칸 선택 후 숫자키로 수행.
public class QuickSlotBar : MonoBehaviour
{
    public const int SlotCount = 5;

    [SerializeField] private Inventory inventory; // 비우면 씬에서 찾음
    [SerializeField] private PlayerStats stats;   // 비우면 씬에서 찾음
    [SerializeField] private KeyCode useKey = KeyCode.R;
    [Tooltip("격자가 열려 있으면 숫자키는 배정용으로 넘김")]
    [SerializeField] private InventoryGridUI grid;

    private readonly ItemDef[] assigned = new ItemDef[SlotCount];
    private int selected = 0;

    public event Action<ItemDef> OnToolUsed;
    public int Selected => selected;

    // 현재 선택된 슬롯에 올려둔 아이템 (없으면 null)
    public ItemDef SelectedDef => assigned[selected];
    public ItemDef GetAssigned(int index)
        => (index >= 0 && index < SlotCount) ? assigned[index] : null;

    private void Start()
    {
        if (inventory == null) inventory = FindObjectOfType<Inventory>();
        if (stats == null) stats = FindObjectOfType<PlayerStats>();
        if (grid == null) grid = FindObjectOfType<InventoryGridUI>();

        if (inventory != null)
        {
            inventory.OnChanged += AutoAssignNewItems;
            AutoAssignNewItems();
        }
    }

    private void OnDestroy()
    {
        if (inventory != null) inventory.OnChanged -= AutoAssignNewItems;
    }

    // 새로 얻은 도구·음식을 빈 퀵슬롯에 자동 등록
    private void AutoAssignNewItems()
    {
        if (inventory == null) return;

        for (int i = 0; i < inventory.SlotCount; i++)
        {
            Inventory.Slot s = inventory.Slots[i];
            if (s.IsEmpty) continue;

            ItemDef def = s.def;
            if (!def.IsTool && !def.IsFood && def.hungerRestore <= 0f) continue;
            if (IsAssigned(def)) continue;

            int empty = FirstEmptySlot();
            if (empty < 0) return; // 빈 슬롯 없음
            assigned[empty] = def;
        }
    }

    private bool IsAssigned(ItemDef def)
    {
        for (int i = 0; i < SlotCount; i++)
            if (assigned[i] == def) return true;
        return false;
    }

    private int FirstEmptySlot()
    {
        for (int i = 0; i < SlotCount; i++)
            if (assigned[i] == null) return i;
        return -1;
    }

    // 같은 아이템이 여러 슬롯에 걸리지 않게: 다른 슬롯에 있던 같은 def는 비움.
    public void Assign(int index, ItemDef def)
    {
        if (index < 0 || index >= SlotCount) return;

        if (def != null)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (i == index) continue;
                if (assigned[i] == def) assigned[i] = null;
            }
        }

        assigned[index] = def;
    }

    private void Update()
    {
        bool gridOpen = grid != null && grid.IsOpen;

        // 격자가 닫혀 있을 때만 숫자키로 선택(열려 있으면 격자가 배정에 사용)
        if (!gridOpen)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (!Input.GetKeyDown(KeyCode.Alpha1 + i)) continue;

                // 이미 선택된 슬롯을 다시 누르면 바로 사용
                if (selected == i) UseSelected();
                else selected = i;
                break;
            }

            if (Input.GetKeyDown(useKey)) UseSelected();
        }
    }

    private void UseSelected()
    {
        if (inventory == null) inventory = FindObjectOfType<Inventory>();

        ItemDef def = assigned[selected];
        if (def == null) { Debug.Log($"[퀵슬롯] {selected + 1}번 비어 있음"); return; }
        if (inventory == null) { Debug.LogWarning("[퀵슬롯] Inventory를 찾지 못함"); return; }

        if (!inventory.Has(def.kind, 1))
        {
            Debug.Log($"[퀵슬롯] {def.displayName} 보유 없음");
            return;
        }

        // 분류가 Food가 아니어도 회복량이 있으면 섭취 가능(설정 실수 방지)
        if (def.hungerRestore > 0f)
        {
            if (stats == null) stats = FindObjectOfType<PlayerStats>();
            if (stats == null) { Debug.LogWarning("[퀵슬롯] PlayerStats를 찾지 못함"); return; }

            inventory.TrySpend(def.kind, 1);
            stats.Eat(def.hungerRestore);
            return;
        }

        if (def.IsTool)
        {
            OnToolUsed?.Invoke(def);
            return;
        }

        Debug.Log($"[퀵슬롯] {def.displayName}은 사용할 수 없음 " +
                  "(음식이면 ItemDef의 Hunger Restore를 0보다 크게 설정)");
    }

    private GUIStyle slotStyle;

    private void OnGUI()
    {
        if (slotStyle == null)
            slotStyle = new GUIStyle(GUI.skin.box) { fontSize = 12, alignment = TextAnchor.MiddleCenter };

        const float w = 62f, h = 46f, gap = 4f;
        float total = SlotCount * w + (SlotCount - 1) * gap;
        float x0 = (Screen.width - total) * 0.5f;
        float y = Screen.height - h - 16f;

        for (int i = 0; i < SlotCount; i++)
        {
            Rect r = new Rect(x0 + i * (w + gap), y, w, h);
            ItemDef def = assigned[i];

            string text;
            if (def == null) text = $"{i + 1}\n-";
            else
            {
                int have = inventory != null ? inventory.Get(def.kind) : 0;
                text = $"{i + 1}\n{def.displayName} {have}";
            }

            Color prev = GUI.color;
            if (i == selected) GUI.color = new Color(1f, 0.95f, 0.6f);
            GUI.Box(r, text, slotStyle);
            GUI.color = prev;
        }
    }
}