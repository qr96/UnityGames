using UnityEngine;

/// <summary>
/// 공격 속도 증가 (쿨다운 감소).
/// </summary>
[CreateAssetMenu(menuName = "Runner/Passives/Attack Speed Up", fileName = "AttackSpeedUpPassive")]
public class AttackSpeedUpPassive : PassiveSkill
{
    [Header("Filter")]
    [Tooltip("어떤 태그의 액티브에 적용할지. 비우면 전체.")]
    public SkillTagFilter filter = new SkillTagFilter();

    [Header("Effect")]
    [Tooltip("한 단계당 쿨다운 배수 (0.9 = 10% 빨라짐)")]
    public float cooldownMultiplierPerStack = 0.9f;

    public override void Apply(int stackLevel)
    {
        if (ModifierRegistry.Instance != null)
            ModifierRegistry.Instance.Register(filter, ModifierType.CooldownMultiplier, cooldownMultiplierPerStack);
    }
}