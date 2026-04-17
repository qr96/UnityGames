using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 장비 상점. 구매 처리 담당.
/// 판매 목록은 인스펙터에서 EquipmentData 리스트로 설정.
/// </summary>
public class Shop : MonoBehaviour
{
    [SerializeField] List<EquipmentData> _stock = new();
    public IReadOnlyList<EquipmentData> Stock => _stock;

    /// <summary>
    /// 장비 구매 시도.
    /// 성공 시 EquipmentInstance 생성 → 옵션 롤링 → 인벤토리 추가.
    /// </summary>
    public bool TryBuy(EquipmentData data)
    {
        if (PlayerGold.Instance == null)
        {
            Debug.LogError("[Shop] PlayerGold 인스턴스가 없습니다.");
            return false;
        }

        if (Inventory.Instance == null)
        {
            Debug.LogError("[Shop] Inventory 인스턴스가 없습니다.");
            return false;
        }

        if (!PlayerGold.Instance.TrySpend(data.buyPrice))
            return false;

        var instance = new EquipmentInstance(data);
        EquipmentOptionRoller.Roll(instance);

        if (!Inventory.Instance.TryAddItem(instance))
        {
            // 인벤토리 가득 찬 경우 골드 환불
            PlayerGold.Instance.Add(data.buyPrice);
            return false;
        }

        return true;
    }
}