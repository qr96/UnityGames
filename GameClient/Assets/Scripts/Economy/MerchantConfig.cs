using System;
using UnityEngine;

// 행상인 주기 방문 + 잉여 장작·식량 매입가.
// 시험판 판매 품목 = 신호탄만 → 판매는 FlareConfig/FlareLauncher에서 처리.
[CreateAssetMenu(fileName = "MerchantConfig", menuName = "혹한/Merchant Config")]
public class MerchantConfig : ScriptableObject
{
    [Header("방문 주기")]
    public float visitIntervalSeconds = 60f;
    public float staySeconds = 20f;

    [Header("매입가")]
    public BuyPrice[] buyPrices;  // 예: 장작, 식량

    [Serializable]
    public struct BuyPrice
    {
        public ItemDef item;
        public int goldPerUnit;
    }
}