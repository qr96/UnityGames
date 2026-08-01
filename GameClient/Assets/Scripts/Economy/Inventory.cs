using System;
using System.Collections.Generic;
using UnityEngine;

// 자원(나뭇가지/장작/식량) + 골드 중앙 보관.
// 노드·제작·화로·상인·신호탄이 이곳을 참조/변경. UI는 OnChanged 구독.
public class Inventory : MonoBehaviour
{
    [Header("시작값")]
    [SerializeField] private int gold = 0;
    [SerializeField] private int startStick = 0;
    [SerializeField] private int startFirewood = 0;
    [SerializeField] private int startFood = 0;

    private readonly Dictionary<ResourceKind, int> counts = new Dictionary<ResourceKind, int>();

    public event Action OnChanged;

    public int Gold => gold;

    private void Awake()
    {
        counts[ResourceKind.Stick]    = startStick;
        counts[ResourceKind.Firewood] = startFirewood;
        counts[ResourceKind.Food]     = startFood;
    }

    public int Get(ResourceKind kind) => counts.TryGetValue(kind, out int v) ? v : 0;

    public void Add(ResourceKind kind, int amount)
    {
        if (amount == 0) return;
        counts[kind] = Mathf.Max(0, Get(kind) + amount);
        OnChanged?.Invoke();
    }

    public bool Has(ResourceKind kind, int amount) => Get(kind) >= amount;

    public bool TrySpend(ResourceKind kind, int amount)
    {
        if (amount < 0 || Get(kind) < amount) return false;
        counts[kind] = Get(kind) - amount;
        OnChanged?.Invoke();
        return true;
    }

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
