using UnityEngine;

/// <summary>
/// 패시브 스킬 정의. ScriptableObject. 런타임 상태는 PassiveSkillInstance가 보유.
/// </summary>
public abstract class PassiveSkill : ScriptableObject
{
    [Header("Display")]
    public string skillName = "New Passive";
    [TextArea(2, 4)]
    public string description = "";
    public Sprite icon;

    [Header("Rules")]
    public int maxStack = 5;

    public abstract void Apply(int stackLevel);
}

/// <summary>
/// 패시브 스킬의 런타임 인스턴스. ISelectableChoice 구현으로 카드 표시 가능.
/// </summary>
public class PassiveSkillInstance : ISelectableChoice
{
    public readonly PassiveSkill definition;
    public int stack;

    public PassiveSkillInstance(PassiveSkill def)
    {
        definition = def;
        stack = 0;
    }

    public string DisplayName => stack > 0
        ? $"{definition.skillName} (Lv.{stack + 1})"
        : definition.skillName;

    public string Description => definition.description;
    public Sprite Icon => definition.icon;

    public bool CanBeOffered => stack < definition.maxStack;

    public void Apply()
    {
        stack++;
        definition.Apply(stack);
        Debug.Log($"[Passive] {definition.skillName} 획득 (Lv.{stack})");
    }
}