using UnityEngine;

public class PriceBalancer
{
    // 장비 강화 시작 비용
    private const long EquipmentStartPrice = 1000;

    // 장비 레벨에 따른 가격 배수
    private const double EquipmentPricePerLevel = 1.8;

    public static long GetInhancePrice(int level)
    {
        var price = EquipmentStartPrice;

        for (int i = 0; i < level - 1; i++)
            price = (long)(price * EquipmentPricePerLevel);

        return price;
    }
}
