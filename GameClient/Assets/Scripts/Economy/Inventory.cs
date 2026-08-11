using System;
using System.Collections.Generic;
using UnityEngine;

// 격자 인벤토리. 칸 수 확장 가능, 칸별 스택 상한(ItemDef). 무게 시스템 없음.
// 아이템은 ItemDef 참조로 다룬다. 문자열 id는 데이터 경계(맵·세이브)에서만 쓰고,
// 그때는 ItemRegistry로 변환해 넘긴다.
public class Inventory : MonoBehaviour
{
    [Serializable]
    public class Slot
    {
        public ItemDef def;   // null = 빈 칸
        public int count;

        public bool IsEmpty => def == null || count <= 0;
        public void Clear() { def = null; count = 0; }
    }

    [Header("조회표 (문자열 id 변환용)")]
    [SerializeField] private ItemRegistry registry;

    [Header("칸")]
    [SerializeField] private int slotCount = 5;

    [Header("시작값")]
    [SerializeField] private int gold = 0;

    private readonly List<Slot> slots = new List<Slot>();
    private int bonusSlotCount;

    public event Action OnChanged;

    public int Gold => gold;
    public IReadOnlyList<Slot> Slots => slots;
    public int SlotCount => slots.Count;
    public ItemRegistry Registry => registry;

    private void Awake() => RebuildSlots();

    private void RebuildSlots()
    {
        int target = slotCount + bonusSlotCount;
        while (slots.Count < target) slots.Add(new Slot());
        while (slots.Count > target) slots.RemoveAt(slots.Count - 1);
    }

    public void AddSlots(int amount)
    {
        if (amount <= 0) return;
        bonusSlotCount += amount;
        RebuildSlots();
        OnChanged?.Invoke();
    }

    // ---- 수량 조회 ----
    public int Get(ItemDef def)
    {
        if (def == null) return 0;

        int sum = 0;
        for (int i = 0; i < slots.Count; i++)
            if (!slots[i].IsEmpty && slots[i].def == def) sum += slots[i].count;
        return sum;
    }

    public bool Has(ItemDef def, int amount) => Get(def) >= amount;

    // 남은 수납 여력(칸·스택 상한 기준)
    public int FreeSpaceFor(ItemDef def)
    {
        if (def == null) return 0;

        int limit = Mathf.Max(1, def.stackLimit);
        int space = 0;
        for (int i = 0; i < slots.Count; i++)
        {
            Slot s = slots[i];
            if (s.IsEmpty) space += limit;
            else if (s.def == def) space += Mathf.Max(0, limit - s.count);
        }
        return space;
    }

    public bool IsFull
    {
        get
        {
            for (int i = 0; i < slots.Count; i++)
                if (slots[i].IsEmpty) return false;
            return true; // 부분 스택 여유는 종류별이므로 FreeSpaceFor로 판단
        }
    }

    // 자동 수납: 같은 종류 스택부터 채우고, 남으면 빈 칸 사용. 실제 수납량 반환.
    public int Add(ItemDef def, int amount)
    {
        if (def == null || amount <= 0) return 0;

        int limit = Mathf.Max(1, def.stackLimit);
        int remain = amount;

        for (int i = 0; i < slots.Count && remain > 0; i++)
        {
            Slot s = slots[i];
            if (s.IsEmpty || s.def != def) continue;
            int room = limit - s.count;
            if (room <= 0) continue;
            int put = Mathf.Min(room, remain);
            s.count += put;
            remain -= put;
        }

        for (int i = 0; i < slots.Count && remain > 0; i++)
        {
            Slot s = slots[i];
            if (!s.IsEmpty) continue;
            int put = Mathf.Min(limit, remain);
            s.def = def;
            s.count = put;
            remain -= put;
        }

        int stored = amount - remain;
        if (stored > 0) OnChanged?.Invoke();
        return stored;
    }

    public bool TrySpend(ItemDef def, int amount)
    {
        if (def == null || amount < 0 || Get(def) < amount) return false;

        int remain = amount;
        for (int i = 0; i < slots.Count && remain > 0; i++)
        {
            Slot s = slots[i];
            if (s.IsEmpty || s.def != def) continue;
            int take = Mathf.Min(s.count, remain);
            s.count -= take;
            remain -= take;
            if (s.count <= 0) s.Clear();
        }
        OnChanged?.Invoke();
        return true;
    }

    // ---- 문자열 id 경로 (맵 로드·세이브용) ----
    public ItemDef Resolve(string itemId)
    {
        if (registry == null)
        {
            Debug.LogWarning("[인벤토리] ItemRegistry가 연결되지 않음");
            return null;
        }

        ItemDef def = registry.Find(itemId);
        if (def == null) Debug.LogWarning($"[인벤토리] 알 수 없는 아이템 id: '{itemId}'");
        return def;
    }

    public int Add(string itemId, int amount) => Add(Resolve(itemId), amount);
    public int Get(string itemId) => Get(Resolve(itemId));

    // ---- 골드 ----
    public void AddGold(int amount)
    {
        gold = Mathf.Max(0, gold + amount);
        OnChanged?.Invoke();
    }

    public bool TrySpendGold(int amount)
    {
        if (amount < 0 || gold < amount) return false;
        gold -= amount;
        OnChanged?.Invoke();
        return true;
    }
}