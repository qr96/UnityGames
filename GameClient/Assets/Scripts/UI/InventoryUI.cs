using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 패널.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("슬롯 그리드")]
    [SerializeField] Transform _slotGrid;
    [SerializeField] InventorySlotUI _slotPrefab;

    [Header("상세 패널")]
    [SerializeField] GameObject _detailPanel;
    [SerializeField] Image _detailIcon;
    [SerializeField] TextMeshProUGUI _detailNameText;
    [SerializeField] TextMeshProUGUI _detailStatsText;
    [SerializeField] Button _equipButton;
    [SerializeField] TextMeshProUGUI _equipButtonText;

    readonly List<InventorySlotUI> _slots = new();
    EquipmentInstance _selectedItem;

    void OnEnable()
    {
        Refresh();
        Inventory.Instance.OnInventoryChanged += Refresh;
        Inventory.Instance.OnEquipmentChanged += Refresh;
    }

    void OnDisable()
    {
        if (Inventory.Instance == null) return;
        Inventory.Instance.OnInventoryChanged -= Refresh;
        Inventory.Instance.OnEquipmentChanged -= Refresh;
    }

    // ── 전체 갱신 ─────────────────────────────────────────────────────────

    void Refresh()
    {
        ClearSlots();

        // 인벤토리 아이템
        foreach (var item in Inventory.Instance.Items)
            CreateSlot(item);

        // 장착 중인 아이템
        foreach (var item in Inventory.Instance.Equipped.Values)
            CreateSlot(item, isEquipped: true);

        // 선택 아이템이 아직 유효한지 확인
        if (_selectedItem != null)
            ShowDetail(_selectedItem);
        else
            _detailPanel.SetActive(false);
    }

    void ClearSlots()
    {
        foreach (var slot in _slots)
            Destroy(slot.gameObject);
        _slots.Clear();
    }

    void CreateSlot(EquipmentInstance item, bool isEquipped = false)
    {
        var slot = Instantiate(_slotPrefab, _slotGrid);
        slot.Setup(item, OnSlotClicked);

        // 장착 중이면 선택 표시
        if (isEquipped) slot.SetSelected(true);

        _slots.Add(slot);
    }

    // ── 슬롯 클릭 ────────────────────────────────────────────────────────

    void OnSlotClicked(EquipmentInstance item)
    {
        _selectedItem = item;
        ShowDetail(item);

        foreach (var slot in _slots)
            slot.SetSelected(slot.Item == item);
    }

    // ── 상세 패널 ─────────────────────────────────────────────────────────

    void ShowDetail(EquipmentInstance item)
    {
        _detailPanel.SetActive(true);

        if (_detailIcon != null && item.data.icon != null)
            _detailIcon.sprite = item.data.icon;

        if (_detailNameText != null)
        {
            string tierName = item.data.tier switch
            {
                EquipmentTier.Normal => "일반",
                EquipmentTier.Rare => "희귀",
                EquipmentTier.Hero => "영웅",
                EquipmentTier.Legend => "전설",
                _ => ""
            };
            _detailNameText.text = $"[{tierName}] {item.data.equipmentName}";
            if (item.enhanceLevel > 0)
                _detailNameText.text += $" +{item.enhanceLevel}";
        }

        if (_detailStatsText != null)
            _detailStatsText.text = BuildStatsText(item);

        // 장착 중인지 확인해서 버튼 텍스트 변경
        bool isEquipped = IsEquipped(item);
        if (_equipButtonText != null)
            _equipButtonText.text = isEquipped ? "해제" : "장착";

        _equipButton.onClick.RemoveAllListeners();
        _equipButton.onClick.AddListener(() => OnEquipButtonClicked(item));
    }

    void OnEquipButtonClicked(EquipmentInstance item)
    {
        if (IsEquipped(item))
            Inventory.Instance.Unequip(item.data.slot);
        else
            Inventory.Instance.TryEquip(item);
    }

    bool IsEquipped(EquipmentInstance item)
    {
        return Inventory.Instance.Equipped.TryGetValue(item.data.slot, out var equipped)
               && equipped == item;
    }

    string BuildStatsText(EquipmentInstance item)
    {
        var sb = new System.Text.StringBuilder();

        // 기본 스탯
        sb.AppendLine(FormatStat(item.data.baseStat, item.GetBaseStatValue()));

        // 옵션
        if (item.options.Count > 0)
        {
            sb.AppendLine("─────────");
            foreach (var option in item.options)
                sb.AppendLine(FormatStat(option.statType, option.value));
        }

        return sb.ToString().TrimEnd();
    }

    string FormatStat(StatType stat, float value)
    {
        return stat switch
        {
            StatType.Attack => $"공격력 +{value:F0}",
            StatType.Defense => $"방어력 +{value:F0}",
            StatType.HP => $"HP +{value:F0}",
            StatType.CriticalChance => $"크리티컬 확률 +{value:F1}%",
            StatType.CriticalDamage => $"크리티컬 데미지 +{value:F1}%",
            StatType.MoveSpeed => $"이동속도 +{value:F1}%",
            StatType.AttackSpeed => $"공격속도 +{value:F1}%",
            _ => ""
        };
    }
}