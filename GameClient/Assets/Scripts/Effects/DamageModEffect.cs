using UnityEngine;

/// <summary>수치류: 데미지 증가/감소. 여러 개 매칭되면 배율 누적곱.</summary>
[CreateAssetMenu(fileName = "FX_DamageMod_", menuName = "Game/Effects/Damage Mod")]
public class DamageModEffect : EffectDefinition
{
    [Tooltip("1.5 = +50%, 0.7 = -30%")]
    public float multiplier = 1.5f;

    public override void OnHit(HitContext ctx)
    {
        ctx.damageMultiplier *= multiplier;
    }
}
