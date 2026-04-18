using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 상점 패널.
///
/// 계층 구조:
/// ShopUI (Panel)
///   ├── GoldText      (TextMeshProUGUI) — 현재 보유 골드
///   └── ItemList      (ScrollView > Viewport > Content)
///        └── ShopItemUI × N (동적 생성)
/// </summary>
public class ShopUI : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] Shop _shop;
    [SerializeField] TextMeshProUGUI _goldText;
    [SerializeField] Transform _itemList;
    [SerializeField] ShopItemUI _itemPrefab;

    readonly List<ShopItemUI> _items = new();

    void OnEnable()
    {
        Refresh();
        PlayerGold.Instance.OnGoldChanged += OnGoldChanged;
    }

    void OnDisable()
    {
        if (PlayerGold.Instance != null)
            PlayerGold.Instance.OnGoldChanged -= OnGoldChanged;
    }

    void Refresh()
    {
        ClearItems();

        foreach (var data in _shop.Stock)
        {
            var item = Instantiate(_itemPrefab, _itemList);
            item.Setup(data, OnBuyClicked);
            _items.Add(item);
        }

        UpdateGoldUI(PlayerGold.Instance != null ? PlayerGold.Instance.Gold : 0);
    }

    void ClearItems()
    {
        foreach (var item in _items)
            Destroy(item.gameObject);
        _items.Clear();
    }

    void OnBuyClicked(EquipmentData data)
    {
        bool success = _shop.TryBuy(data);

        if (!success)
            Debug.Log("[ShopUI] 구매 실패 (골드 부족 or 인벤토리 가득)");
    }

    void OnGoldChanged(int gold)
    {
        UpdateGoldUI(gold);
    }

    void UpdateGoldUI(int gold)
    {
        if (_goldText != null)
            _goldText.text = $"{gold:N0} G";

        for (int i = 0; i < _items.Count; i++)
            _items[i].SetAffordable(gold >= _shop.Stock[i].buyPrice);
    }
}