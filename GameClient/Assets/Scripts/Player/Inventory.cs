using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 인벤토리. 아이템 보관 + 장착 슬롯 관리.
/// </summary>
public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }

    [Header("설정")]
    [SerializeField] int _maxCapacity = 50;

    // ── 아이템 목록 ───────────────────────────────────────────────────────
    readonly List<EquipmentInstance> _items = new();
    public IReadOnlyList<EquipmentInstance> Items => _items;

    // ── 장착 슬롯 (슬롯당 1개) ────────────────────────────────────────────
    readonly Dictionary<EquipmentSlot, EquipmentInstance> _equipped = new();
    public IReadOnlyDictionary<EquipmentSlot, EquipmentInstance> Equipped => _equipped;

    // ── 이벤트 ────────────────────────────────────────────────────────────
    public event Action OnInventoryChanged;
    public event Action OnEquipmentChanged;

    // ─────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── 아이템 추가/제거 ──────────────────────────────────────────────────

    public bool TryAddItem(EquipmentInstance item)
    {
        if (_items.Count >= _maxCapacity)
        {
            Debug.LogWarning("[Inventory] 인벤토리가 가득 찼습니다.");
            return false;
        }

        _items.Add(item);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveItem(EquipmentInstance item)
    {
        if (!_items.Remove(item)) return false;
        OnInventoryChanged?.Invoke();
        return true;
    }

    // ── 장착 / 탈착 ───────────────────────────────────────────────────────

    /// <summary>
    /// 장비 장착. 같은 슬롯에 이미 장착된 장비는 인벤토리로 돌아옴.
    /// 인벤토리에 없는 아이템은 장착 불가.
    /// </summary>
    public bool TryEquip(EquipmentInstance item)
    {
        if (!_items.Contains(item))
        {
            Debug.LogWarning("[Inventory] 인벤토리에 없는 아이템은 장착할 수 없습니다.");
            return false;
        }

        var slot = item.data.slot;

        // 기존 장착 장비 탈착
        if (_equipped.TryGetValue(slot, out var current))
            Unequip(slot, addToInventory: false); // 이미 인벤토리에 있으므로 재추가 불필요

        // 장착
        _items.Remove(item);
        _equipped[slot] = item;
        ApplyModifiers(item);

        OnEquipmentChanged?.Invoke();
        OnInventoryChanged?.Invoke();
        return true;
    }

    public void Unequip(EquipmentSlot slot, bool addToInventory = true)
    {
        if (!_equipped.TryGetValue(slot, out var item)) return;

        RemoveModifiers(item);
        _equipped.Remove(slot);

        if (addToInventory)
        {
            _items.Add(item);
            OnInventoryChanged?.Invoke();
        }

        OnEquipmentChanged?.Invoke();
    }

    // ── StatModifier 연동 ─────────────────────────────────────────────────

    void ApplyModifiers(EquipmentInstance item)
    {
        if (PlayerStats.Instance == null) return;

        string id = item.GetModifierId();

        // 중복 방지: 먼저 제거 후 추가
        PlayerStats.Instance.RemoveModifiersById(id);

        // 기본 스탯
        PlayerStats.Instance.AddModifier(new StatModifier(
            id, item.data.baseStat, item.GetBaseStatValue(), ModifierSource.Equipment));

        // 랜덤 옵션
        foreach (var option in item.options)
            PlayerStats.Instance.AddModifier(new StatModifier(
                id, option.statType, option.value, ModifierSource.Equipment));
    }

    void RemoveModifiers(EquipmentInstance item)
    {
        PlayerStats.Instance?.RemoveModifiersById(item.GetModifierId());
    }
}