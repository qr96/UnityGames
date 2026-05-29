using UnityEngine;

[CreateAssetMenu(menuName = "Runner/Passives/Magnet Range Up", fileName = "MagnetRangeUpPassive")]
public class MagnetRangeUpPassive : PassiveSkill
{
    [Tooltip("한 단계당 자석 범위에 더해질 값(미터)")]
    public float rangePerStack = 1f;

    public override void Apply(int stackLevel)
    {
        Coin.GlobalMagnetBonus += rangePerStack;
    }
}
