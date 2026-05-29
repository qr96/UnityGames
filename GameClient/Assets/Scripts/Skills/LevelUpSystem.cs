using System.Collections.Generic;
using UnityEngine;

public class LevelUpSystem : MonoBehaviour
{
    public static LevelUpSystem Instance { get; private set; }

    [Header("Passive Skill Pool")]
    public List<PassiveSkill> allPassives = new List<PassiveSkill>();

    [Header("Selection")]
    public int choicesPerLevelUp = 3;

    [Header("UI")]
    public SkillSelectionUI selectionUI;

    private readonly Dictionary<PassiveSkill, PassiveSkillInstance> passiveInstances = new();
    private bool subscribed = false;

    void Awake()
    {
        Instance = this;

        // 재시작 시 전역 상태 초기화
        Coin.GlobalMagnetBonus = 0f;
        if (ModifierRegistry.Instance != null) ModifierRegistry.Instance.Clear();

        foreach (var p in allPassives)
        {
            if (p != null && !passiveInstances.ContainsKey(p))
                passiveInstances.Add(p, new PassiveSkillInstance(p));
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start() => TrySubscribe();
    void OnEnable() => TrySubscribe();

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
        if (subscribed || ExperienceManager.Instance == null) return;
        ExperienceManager.Instance.OnLevelUp += HandleLevelUp;
        subscribed = true;
    }

    void HandleLevelUp(int newLevel)
    {
        var choices = RollChoices();
        if (choices.Count == 0)
        {
            Debug.LogWarning("[LevelUp] 후보가 없음");
            return;
        }
        if (selectionUI != null) selectionUI.Show(choices, OnChoiceSelected);
        else Debug.LogError("[LevelUp] selectionUI가 null");
    }

    List<ISelectableChoice> RollChoices()
    {
        var pool = new List<ISelectableChoice>();

        foreach (var inst in passiveInstances.Values)
            if (inst.CanBeOffered) pool.Add(inst);

        if (ActiveSkillSystem.Instance != null)
        {
            foreach (var active in ActiveSkillSystem.Instance.activeSkills)
            {
                if (active == null) continue;
                var choice = active.AsChoice();
                if (choice.CanBeOffered) pool.Add(choice);
            }
        }

        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        int take = Mathf.Min(choicesPerLevelUp, pool.Count);
        return pool.GetRange(0, take);
    }

    void OnChoiceSelected(ISelectableChoice chosen) => chosen.Apply();
}