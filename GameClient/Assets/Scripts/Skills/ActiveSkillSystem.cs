using System.Collections.Generic;
using UnityEngine;

public class ActiveSkillSystem : MonoBehaviour
{
    public static ActiveSkillSystem Instance { get; private set; }

    [Header("보유 액티브 스킬")]
    [Tooltip("자식에 활성 상태로 부착된 ActiveSkill 컴포넌트를 자동 수집. 비활성 GameObject의 ActiveSkill은 제외됨.")]
    public List<ActiveSkill> activeSkills = new List<ActiveSkill>();

    void Awake()
    {
        Instance = this;

        // 활성 상태의 자식 ActiveSkill만 자동 수집.
        // 비활성 GameObject = "지금은 사용하지 않는 액티브"로 취급.
        var found = GetComponentsInChildren<ActiveSkill>(false);
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

    /// <summary>새 액티브 스킬 동적 추가 (레벨업으로 신규 액티브 획득 등).</summary>
    public void AddActiveSkill(ActiveSkill skill)
    {
        if (!activeSkills.Contains(skill)) activeSkills.Add(skill);
        if (!skill.gameObject.activeSelf) skill.gameObject.SetActive(true);
    }
}