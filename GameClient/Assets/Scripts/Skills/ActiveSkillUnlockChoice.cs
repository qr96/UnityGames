using UnityEngine;

/// <summary>
/// 아직 보유하지 않은 액티브 스킬을 획득하는 레벨업 선택지.
/// 선택하면 해당 액티브가 활성화되어 발동 시작.
/// </summary>
public class ActiveSkillUnlockChoice : ISelectableChoice
{
    private readonly ActiveSkill skill;

    public ActiveSkillUnlockChoice(ActiveSkill skill)
    {
        this.skill = skill;
    }

    // 신규 획득임을 표시 (레벨업과 구분)
    public string DisplayName => $"{skill.displayName} (신규!)";
    public string Description => skill.description;
    public Sprite Icon => skill.icon;

    // 미보유 상태일 때만 후보 (이미 보유하면 레벨업 쪽에서 다룸)
    public bool CanBeOffered => skill != null && !skill.gameObject.activeInHierarchy;

    public void Apply()
    {
        if (ActiveSkillSystem.Instance != null)
            ActiveSkillSystem.Instance.UnlockSkill(skill);
    }
}
