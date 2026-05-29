using UnityEngine;

/// <summary>
/// 액티브 스킬을 레벨업 카드 선택지로 표현하는 래퍼.
/// ActiveSkill 자체는 공격 로직만 갖고, 선택지로서의 표현은 이 클래스가 담당.
/// </summary>
public class ActiveSkillLevelUpChoice : ISelectableChoice
{
    public readonly ActiveSkill skill;
    public readonly string displayName;
    public readonly string description;
    public readonly Sprite icon;

    public ActiveSkillLevelUpChoice(ActiveSkill skill, string displayName, string description, Sprite icon)
    {
        this.skill = skill;
        this.displayName = displayName;
        this.description = description;
        this.icon = icon;
    }

    public string DisplayName => $"{displayName} (Lv.{skill.level + 1})";
    public string Description => description;
    public Sprite Icon => icon;

    public bool CanBeOffered => skill.CanLevelUp;

    public void Apply()
    {
        skill.LevelUp();
        Debug.Log($"[Active] {displayName} 레벨업 → Lv.{skill.level}");
    }
}
