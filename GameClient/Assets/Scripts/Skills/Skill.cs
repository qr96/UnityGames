using UnityEngine;

/// <summary>
/// 스킬 정의. ScriptableObject라서 에셋으로 관리 가능. 런타임 상태는 안 가짐.
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

/// <summary>
/// 런타임 인스턴스. 플레이마다 새로 만들어짐.
/// </summary>
public class SkillInstance
{
    public readonly Skill definition;
    public int stack;

    public SkillInstance(Skill def) { definition = def; stack = 0; }

    public bool CanLevelUp => stack < definition.maxStack;
}
