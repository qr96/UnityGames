using UnityEngine;

// 신호탄 가격. 구매할 때마다 상승.
[CreateAssetMenu(fileName = "FlareConfig", menuName = "혹한/Flare Config")]
public class FlareConfig : ScriptableObject
{
    public int basePrice = 50;
    [Tooltip("구매 1회마다 곱해지는 배수 (1.5 = 매회 50% 상승)")]
    public float priceMultiplierPerPurchase = 1.5f;

    // 지금까지 구매 횟수를 넣으면 다음 구매 가격 반환
    public int PriceForPurchaseIndex(int purchasesSoFar)
    {
        return Mathf.RoundToInt(basePrice * Mathf.Pow(priceMultiplierPerPurchase, purchasesSoFar));
    }
}
