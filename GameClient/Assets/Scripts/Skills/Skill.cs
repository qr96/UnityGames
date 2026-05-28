using UnityEngine;

/// <summary>
/// 스킬 정의. ScriptableObject라서 에셋으로 관리. 런타임 상태는 SkillInstance가 보유.
/// </summary>
public abstract class Skill : ScriptableObject
{
    [Header("Display")]
    public string skillName = "New Skill";
    [TextArea(2, 4)]
    public string description = "";
    public Sprite icon;

    [Header("Rules")]
    public int maxStack = 5;

    public abstract void Apply(int stackLevel);
}

public class SkillInstance
{
    public readonly Skill definition;
    public int stack;

    public SkillInstance(Skill def) { definition = def; stack = 0; }

    public bool CanLevelUp => stack < definition.maxStack;
}