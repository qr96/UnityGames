using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상점 아이템 행 하나. ShopUI에서 동적으로 생성.
/// </summary>
public class ShopItemUI : MonoBehaviour
{
    [SerializeField] Image _icon;
    [SerializeField] TextMeshProUGUI _nameText;
    [SerializeField] TextMeshProUGUI _priceText;
    [SerializeField] Button _buyButton;

    public void Setup(EquipmentData data, Action<EquipmentData> onBuy)
    {
        if (_icon != null && data.icon != null)
            _icon.sprite = data.icon;

        if (_nameText != null)
            _nameText.text = data.equipmentName;

        if (_priceText != null)
            _priceText.text = $"{data.buyPrice:N0} G";

        _buyButton.onClick.RemoveAllListeners();
        _buyButton.onClick.AddListener(() => onBuy?.Invoke(data));
    }

    /// <summary>골드 부족 시 버튼 비활성화.</summary>
    public void SetAffordable(bool affordable)
    {
        _buyButton.interactable = affordable;
        if (_priceText != null)
            _priceText.color = affordable
                ? new Color(1f, 0.85f, 0.2f)  // 골드색
                : new Color(0.6f, 0.6f, 0.6f); // 회색
    }
}