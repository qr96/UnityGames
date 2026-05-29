using UnityEngine;

/// <summary>
/// 공격력 증가. 태그 필터로 어떤 액티브에 적용될지 지정 가능.
/// 필터 비워두면 모든 액티브에 적용.
/// </summary>
[CreateAssetMenu(menuName = "Runner/Passives/Damage Up", fileName = "DamageUpPassive")]
public class DamageUpPassive : PassiveSkill
{
    [Header("Filter")]
    [Tooltip("어떤 태그의 액티브에 적용할지. 비우면 전체.")]
    public SkillTagFilter filter = new SkillTagFilter();

    [Header("Effect")]
    [Tooltip("한 단계당 데미지 배수 (1.15 = 15% 증가)")]
    public float multiplierPerStack = 1.15f;

    public override void Apply(int stackLevel)
    {
        if (ModifierRegistry.Instance != null)
            ModifierRegistry.Instance.Register(filter, ModifierType.DamageMultiplier, multiplierPerStack);
    }
}