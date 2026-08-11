using UnityEngine;

// 핫바 = 장비 전용 (도끼·곡괭이·창·활·횃불). 음식·재료·설치물은 올라가지 않는다.
// 숫자키 1~5 = 그 장비 즉시 들기(이미 든 것이면 아무 일 없음). Q = 순환.
// 사용(스윙·발사)은 AttackExecutor가 스페이스로 처리한다 — 핫바는 '무엇을 들었는가'만 관리.
public class Hotbar : MonoBehaviour
{
    public const int SlotCount = 5;

    [SerializeField] private KeyCode cycleKey = KeyCode.Q;
    [SerializeField] private Inventory inventory; // 비우면 씬에서 찾음

    private readonly ItemDef[] assigned = new ItemDef[SlotCount];
    private int equippedIndex;

    public int EquippedIndex => equippedIndex;
    public ItemDef GetAssigned(int index)
        => (index >= 0 && index < SlotCount) ? assigned[index] : null;

    // 지금 손에 든 장비. 슬롯이 비었거나 인벤토리에 없으면 null(빈손).
    public ItemDef EquippedItem
    {
        get
        {
            ItemDef def = assigned[equippedIndex];
            if (def == null || inventory == null) return null;
            return inventory.Has(def, 1) ? def : null;
        }
    }

    private void Start()
    {
        if (inventory == null) inventory = FindObjectOfType<Inventory>();
        if (inventory != null)
        {
            inventory.OnChanged += AutoAssign;
            AutoAssign();
        }
    }

    private void OnDestroy()
    {
        if (inventory != null) inventory.OnChanged -= AutoAssign;
    }

    // 새로 얻은 장비를 빈 슬롯에 자동 등록
    private void AutoAssign()
    {
        if (inventory == null) return;

        for (int i = 0; i < inventory.SlotCount; i++)
        {
            Inventory.Slot s = inventory.Slots[i];
            if (s.IsEmpty || !s.def.IsEquipment) continue;
            if (IndexOf(s.def) >= 0) continue;

            int empty = FirstEmpty();
            if (empty < 0) return;
            assigned[empty] = s.def;
        }
    }

    private int IndexOf(ItemDef def)
    {
        for (int i = 0; i < SlotCount; i++) if (assigned[i] == def) return i;
        return -1;
    }

    private int FirstEmpty()
    {
        for (int i = 0; i < SlotCount; i++) if (assigned[i] == null) return i;
        return -1;
    }

    // 장비만 배정 가능. 다른 슬롯에 같은 장비가 있으면 비운다.
    public bool Assign(int index, ItemDef def)
    {
        if (index < 0 || index >= SlotCount) return false;
        if (def != null && !def.IsEquipment) return false;

        if (def != null)
            for (int i = 0; i < SlotCount; i++)
                if (i != index && assigned[i] == def) assigned[i] = null;

        assigned[index] = def;
        return true;
    }

    public void Equip(int index)
    {
        if (index < 0 || index >= SlotCount) return;
        equippedIndex = index; // 이미 든 것이면 결과적으로 변화 없음
    }

    private void Cycle()
    {
        for (int step = 1; step <= SlotCount; step++)
        {
            int next = (equippedIndex + step) % SlotCount;
            if (assigned[next] != null) { equippedIndex = next; return; }
        }
    }

    private void Update()
    {
        if (UIInputLock.IsBlocked) return;

        for (int i = 0; i < SlotCount; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i)) { Equip(i); return; }

        if (Input.GetKeyDown(cycleKey)) Cycle();
    }


}