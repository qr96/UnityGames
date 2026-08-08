using UnityEngine;

// F = 퀵푸드. 지정된 음식을 즉시 섭취한다.
// 지정은 인벤토리 격자에서 F로 하고, 미지정이면 인벤토리의 첫 음식이 자동 지정된다.
// 지정한 음식이 떨어지면 같은 종류가 다시 생길 때까지 다른 음식으로 자동 재지정.
public class QuickFood : MonoBehaviour
{
    [SerializeField] private KeyCode eatKey = KeyCode.F;
    [SerializeField] private Inventory inventory; // 비우면 씬에서 찾음
    [SerializeField] private PlayerStats stats;   // 비우면 씬에서 찾음

    private ItemDef assigned;

    public ItemDef Assigned => assigned;
    public int AssignedCount => (assigned != null && inventory != null) ? inventory.Get(assigned.kind) : 0;

    private void Start()
    {
        if (inventory == null) inventory = FindObjectOfType<Inventory>();
        if (stats == null) stats = FindObjectOfType<PlayerStats>();

        if (inventory != null)
        {
            inventory.OnChanged += EnsureAssigned;
            EnsureAssigned();
        }
    }

    private void OnDestroy()
    {
        if (inventory != null) inventory.OnChanged -= EnsureAssigned;
    }

    private static bool IsFood(ItemDef def)
        => def != null && (def.IsFood || def.hungerRestore > 0f);

    // 격자 UI에서 호출 — 음식만 지정 가능
    public bool Assign(ItemDef def)
    {
        if (!IsFood(def)) return false;
        assigned = def;
        return true;
    }

    // 미지정이거나 지정한 음식이 떨어졌으면 인벤토리의 첫 음식으로 재지정
    private void EnsureAssigned()
    {
        if (inventory == null) return;
        if (assigned != null && inventory.Has(assigned.kind, 1)) return;

        for (int i = 0; i < inventory.SlotCount; i++)
        {
            Inventory.Slot s = inventory.Slots[i];
            if (s.IsEmpty || !IsFood(s.def)) continue;
            assigned = s.def;
            return;
        }

        assigned = null;
    }

    private void Update()
    {
        if (UIInputLock.IsBlocked) return;
        if (!Input.GetKeyDown(eatKey)) return;

        Eat();
    }

    public void Eat()
    {
        if (inventory == null) inventory = FindObjectOfType<Inventory>();
        if (assigned == null) { Debug.Log("[퀵푸드] 지정된 음식 없음"); return; }
        if (inventory == null || !inventory.Has(assigned.kind, 1))
        {
            Debug.Log($"[퀵푸드] {assigned.displayName} 없음");
            EnsureAssigned();
            return;
        }

        if (stats == null) stats = FindObjectOfType<PlayerStats>();
        if (stats == null) { Debug.LogWarning("[퀵푸드] PlayerStats를 찾지 못함"); return; }

        inventory.TrySpend(assigned.kind, 1);
        stats.Eat(assigned.hungerRestore);
        EnsureAssigned(); // 소진 시 자동 재지정
    }

    private GUIStyle slotStyle;

    private void OnGUI()
    {
        if (slotStyle == null)
            slotStyle = new GUIStyle(GUI.skin.box) { fontSize = 12, alignment = TextAnchor.MiddleCenter };

        const float w = 92f, h = 46f;
        float x = (Screen.width * 0.5f) + (Hotbar.SlotCount * 68f + (Hotbar.SlotCount - 1) * 4f) * 0.5f + 12f;
        float y = Screen.height - h - 16f;

        string text = assigned == null
            ? $"{eatKey}\n음식 없음"
            : $"{eatKey}\n{assigned.displayName} {AssignedCount}";

        GUI.Box(new Rect(x, y, w, h), text, slotStyle);
    }
}
