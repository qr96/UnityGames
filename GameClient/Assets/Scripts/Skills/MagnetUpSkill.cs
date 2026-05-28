using UnityEngine;

[CreateAssetMenu(menuName = "Runner/Skills/Magnet Up", fileName = "MagnetUpSkill")]
public class MagnetUpSkill : Skill
{
    [Tooltip("한 단계당 자석 범위에 더해질 값(미터)")]
    public float rangeBonus = 1f;

    public override void Apply(int stackLevel)
    {
        Coin.GlobalMagnetBonus += rangeBonus;
    }
}
