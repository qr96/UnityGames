using System;
using System.Collections.Generic;
using UnityEngine;

// 격자 인벤토리. 칸 수 확장 가능, 칸별 스택 상한(ItemDef). 무게 시스템 없음(v0.7).
// 골드는 칸에 포함하지 않음.
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

    [Header("조회표")]
    [SerializeField] private ItemDatabase database;

    [Header("칸")]
    [SerializeField] private int slotCount = 10;

    [Header("시작값")]
    [SerializeField] private int gold = 0;

    private readonly List<Slot> slots = new List<Slot>();
    private int bonusSlotCount;       // 확장분

    public event Action OnChanged;

    public int Gold => gold;
    public IReadOnlyList<Slot> Slots => slots;
    public int SlotCount => slots.Count;
    public ItemDatabase Database => database;

    private void Awake()
    {
        RebuildSlots();
    }

    private void RebuildSlots()
    {
        int target = slotCount + bonusSlotCount;
        while (slots.Count < target) slots.Add(new Slot());
        while (slots.Count > target) slots.RemoveAt(slots.Count - 1);
    }

    // ---- 확장 진입점 ----
    public void AddSlots(int amount)
    {
        if (amount <= 0) return;
        bonusSlotCount += amount;
        RebuildSlots();
        OnChanged?.Invoke();
    }

    // ---- 수량 조회 ----
    public int Get(ResourceKind kind)
    {
        int sum = 0;
        for (int i = 0; i < slots.Count; i++)
            if (!slots[i].IsEmpty && slots[i].def.kind == kind) sum += slots[i].count;
        return sum;
    }

    public bool Has(ResourceKind kind, int amount) => Get(kind) >= amount;

    // 남은 수납 여력(칸·스택 상한 기준).
    public int FreeSpaceFor(ResourceKind kind)
    {
        ItemDef def = database != null ? database.Find(kind) : null;
        if (def == null)
        {
            // 여기서 0이 되면 "칸 없음"처럼 보이므로 원인을 명시
            Debug.LogWarning(database == null
                ? "[Inventory] ItemDatabase가 연결되지 않음"
                : $"[Inventory] ItemDatabase에 {kind} 정의가 없음");
            return 0;
        }

        int space = 0;
        for (int i = 0; i < slots.Count; i++)
        {
            Slot s = slots[i];
            if (s.IsEmpty) space += def.stackLimit;
            else if (s.def == def) space += Mathf.Max(0, def.stackLimit - s.count);
        }
        return space;
    }

    public bool IsFull
    {
        get
        {
            for (int i = 0; i < slots.Count; i++)
                if (slots[i].IsEmpty) return false;
            return true; // 부분 스택 여유는 종류별로 다르므로 FreeSpaceFor로 판단
        }
    }

    // 자동 수납: 같은 종류 스택부터 채우고, 남으면 빈 칸 사용. 실제 수납량 반환.
    public int Add(ResourceKind kind, int amount)
    {
        if (amount <= 0) return 0;
        ItemDef def = database != null ? database.Find(kind) : null;
        if (def == null)
        {
            Debug.LogWarning($"[Inventory] ItemDatabase에 {kind} 정의가 없음");
            return 0;
        }

        int remain = amount;

        // 1) 기존 스택 채우기
        for (int i = 0; i < slots.Count && remain > 0; i++)
        {
            Slot s = slots[i];
            if (s.IsEmpty || s.def != def) continue;
            int room = def.stackLimit - s.count;
            if (room <= 0) continue;
            int put = Mathf.Min(room, remain);
            s.count += put;
            remain -= put;
        }

        // 2) 빈 칸 사용
        for (int i = 0; i < slots.Count && remain > 0; i++)
        {
            Slot s = slots[i];
            if (!s.IsEmpty) continue;
            int put = Mathf.Min(def.stackLimit, remain);
            s.def = def;
            s.count = put;
            remain -= put;
        }

        int stored = amount - remain;
        if (stored > 0) OnChanged?.Invoke();
        return stored;
    }

    public bool TrySpend(ResourceKind kind, int amount)
    {
        if (amount < 0 || Get(kind) < amount) return false;

        int remain = amount;
        for (int i = 0; i < slots.Count && remain > 0; i++)
        {
            Slot s = slots[i];
            if (s.IsEmpty || s.def.kind != kind) continue;
            int take = Mathf.Min(s.count, remain);
            s.count -= take;
            remain -= take;
            if (s.count <= 0) s.Clear();
        }
        OnChanged?.Invoke();
        return true;
    }

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