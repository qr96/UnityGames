using UnityEngine;

// 추위(온기)+허기 튜닝값. PlayerStats가 이 값을 읽도록 연결 가능(선택).
[CreateAssetMenu(fileName = "SurvivalConfig", menuName = "혹한/Survival Config")]
public class SurvivalConfig : ScriptableObject
{
    [Header("온기 (추위)")]
    public float maxWarmth = 100f;
    public float warmthDrainPerSec = 5f;    // 화로 밖
    public float warmthChargePerSec = 25f;  // 화로 안(균일)
    public float recoverWarmthThreshold = 40f;

    [Header("허기")]
    public float maxHunger = 100f;
    public float hungerDrainPerSec = 2f;
}
