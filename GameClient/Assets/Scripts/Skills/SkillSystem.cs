using System.Collections.Generic;
using UnityEngine;

public class SkillSystem : MonoBehaviour
{
    public static SkillSystem Instance { get; private set; }

    [Header("All Available Skills")]
    public List<Skill> allSkills = new List<Skill>();

    [Header("Selection")]
    public int choicesPerLevelUp = 3;

    [Header("UI")]
    public SkillSelectionUI selectionUI;

    private readonly Dictionary<Skill, SkillInstance> instances = new Dictionary<Skill, SkillInstance>();

    void Awake()
    {
        Instance = this;

        foreach (var skill in allSkills)
        {
            if (skill != null && !instances.ContainsKey(skill))
                instances.Add(skill, new SkillInstance(skill));
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void OnEnable()
    {
        if (ExperienceManager.Instance != null)
            ExperienceManager.Instance.OnLevelUp += HandleLevelUp;
    }

    void OnDisable()
    {
        if (ExperienceManager.Instance != null)
            ExperienceManager.Instance.OnLevelUp -= HandleLevelUp;
    }

    void HandleLevelUp(int newLevel)
    {
        var choices = RollChoices();
        if (choices.Count == 0) return;
        if (selectionUI != null) selectionUI.Show(choices, OnSkillChosen);
    }

    List<SkillInstance> RollChoices()
    {
        var pool = new List<SkillInstance>();
        foreach (var inst in instances.Values)
            if (inst.CanLevelUp) pool.Add(inst);

        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        int take = Mathf.Min(choicesPerLevelUp, pool.Count);
        return pool.GetRange(0, take);
    }

    void OnSkillChosen(SkillInstance chosen)
    {
        chosen.stack++;
        chosen.definition.Apply(chosen.stack);
        Debug.Log($"[Skill] {chosen.definition.skillName} 획득 (Lv.{chosen.stack})");
    }
}
