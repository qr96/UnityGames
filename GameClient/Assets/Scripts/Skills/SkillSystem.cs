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
    private bool subscribed = false;

    void Awake()
    {
        Instance = this;

        Coin.GlobalMagnetBonus = 0f;

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

    // Start는 모든 GameObject의 Awake가 끝난 뒤 호출되므로 Instance 보장됨
    void Start()
    {
        TrySubscribe();
    }

    void OnEnable()
    {
        TrySubscribe();
    }

    void OnDisable()
    {
        if (subscribed && ExperienceManager.Instance != null)
        {
            ExperienceManager.Instance.OnLevelUp -= HandleLevelUp;
            subscribed = false;
        }
    }

    void TrySubscribe()
    {
        if (subscribed) return;
        if (ExperienceManager.Instance == null) return;

        ExperienceManager.Instance.OnLevelUp += HandleLevelUp;
        subscribed = true;
        Debug.Log("[Skill] OnLevelUp 구독 완료");
    }

    void HandleLevelUp(int newLevel)
    {
        Debug.Log($"[Skill] 레벨업 감지: Lv.{newLevel}");
        var choices = RollChoices();
        if (choices.Count == 0)
        {
            Debug.LogWarning("[Skill] 선택할 스킬이 없음");
            return;
        }
        if (selectionUI != null)
        {
            selectionUI.Show(choices, OnSkillChosen);
        }
        else
        {
            Debug.LogError("[Skill] selectionUI가 null! Inspector 연결 확인");
        }
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