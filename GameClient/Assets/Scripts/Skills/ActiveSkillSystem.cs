using System.Collections.Generic;
using UnityEngine;

public class ActiveSkillSystem : MonoBehaviour
{
    public static ActiveSkillSystem Instance { get; private set; }

    [Header("초기 보유 액티브 스킬")]
    public List<ActiveSkill> activeSkills = new List<ActiveSkill>();

    void Awake()
    {
        Instance = this;

        var found = GetComponentsInChildren<ActiveSkill>(true);
        foreach (var s in found)
            if (!activeSkills.Contains(s)) activeSkills.Add(s);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (PlayerController.PlayerTransform == null) return;

        Vector3 playerPos = PlayerController.PlayerTransform.position;
        float dt = Time.deltaTime;

        foreach (var skill in activeSkills)
        {
            if (skill != null && skill.enabled) skill.Tick(dt, playerPos);
        }
    }

    public void AddActiveSkill(ActiveSkill skill)
    {
        if (!activeSkills.Contains(skill)) activeSkills.Add(skill);
    }
}