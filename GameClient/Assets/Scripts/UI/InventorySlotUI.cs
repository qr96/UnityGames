using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 슬롯 하나. InventoryUI에서 동적으로 생성.
/// </summary>
public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] Image _icon;
    [SerializeField] Image _tierBorder;
    [SerializeField] TextMeshProUGUI _enhanceText;
    [SerializeField] Button _button;

    public EquipmentInstance Item { get; private set; }

    static readonly Color[] TierColors = {
        new Color(0.8f, 0.8f, 0.8f), // 일반  — 회색
        new Color(0.2f, 0.6f, 1f),   // 희귀  — 파랑
        new Color(0.6f, 0.2f, 1f),   // 영웅  — 보라
        new Color(1f,   0.7f, 0.1f), // 전설  — 금색
    };

    public void Setup(EquipmentInstance item, Action<EquipmentInstance> onClicked)
    {
        Item = item;

        if (_icon != null && item.data.icon != null)
            _icon.sprite = item.data.icon;

        if (_tierBorder != null)
            _tierBorder.color = TierColors[(int)item.data.tier - 1];

        if (_enhanceText != null)
            _enhanceText.text = item.enhanceLevel > 0 ? $"+{item.enhanceLevel}" : "";

        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(() => onClicked?.Invoke(item));
    }

    public void SetSelected(bool selected)
    {
        _button.targetGraphic.color = selected
            ? new Color(1f, 1f, 0.5f)
            : Color.white;
    }
}
