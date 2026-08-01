using System;
using UnityEngine;

// 화로 기본값 + 강화 단계. 강화 효과 = 장작 용량 + 온기 반경 확장.
[CreateAssetMenu(fileName = "HearthConfig", menuName = "혹한/Hearth Config")]
public class HearthConfig : ScriptableObject
{
    [Header("기본")]
    public float baseWarmthRadius = 5f;
    public float baseFuelCapacity = 100f;  // 장작 용량
    public float fuelBurnPerSec = 1f;       // 켜져 있을 때 소모

    [Header("강화 단계 (인덱스 = 레벨)")]
    public UpgradeStep[] upgrades;

    [Serializable]
    public struct UpgradeStep
    {
        public int goldCost;
        public ResourceKind resourceCost;   // 소모 자원 종류
        public int resourceAmount;
        public float addedFuelCapacity;     // 장작 용량 증가
        public float addedWarmthRadius;     // 온기 반경 확장(눈 물러남)
    }
}
