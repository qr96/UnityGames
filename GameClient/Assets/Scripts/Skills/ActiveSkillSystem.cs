using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 액티브 스킬 관리.
/// - 전체 액티브(allActiveSkills): 자식 전부. 활성=보유 중, 비활성=미보유.
/// - 보유 중(활성)인 것만 매 프레임 Tick.
/// 미보유 액티브는 획득 시스템(레벨업 카드)을 통해 활성화될 수 있음.
/// </summary>
public class ActiveSkillSystem : MonoBehaviour
{
    public static ActiveSkillSystem Instance { get; private set; }

    [Header("전체 액티브 스킬 (자식 자동 수집, 활성+비활성 모두)")]
    public List<ActiveSkill> allActiveSkills = new List<ActiveSkill>();

    void Awake()
    {
        Instance = this;

        // 자식의 모든 ActiveSkill 수집 (비활성 포함 — 미보유 액티브 추적용)
        var found = GetComponentsInChildren<ActiveSkill>(true);
        foreach (var s in found)
            if (!allActiveSkills.Contains(s)) allActiveSkills.Add(s);
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

        foreach (var skill in allActiveSkills)
        {
            // 활성(보유 중)인 것만 발동
            if (skill != null && skill.gameObject.activeInHierarchy && skill.enabled)
                skill.Tick(dt, playerPos);
        }
    }

    /// <summary>현재 보유 중(활성)인 액티브 스킬들.</summary>
    public IEnumerable<ActiveSkill> GetOwnedSkills()
    {
        foreach (var s in allActiveSkills)
            if (s != null && s.gameObject.activeInHierarchy) yield return s;
    }

    /// <summary>아직 미보유(비활성)인 액티브 스킬들. 획득 후보.</summary>
    public IEnumerable<ActiveSkill> GetUnownedSkills()
    {
        foreach (var s in allActiveSkills)
            if (s != null && !s.gameObject.activeInHierarchy) yield return s;
    }

    /// <summary>액티브 스킬 획득 (비활성 → 활성).</summary>
    public void UnlockSkill(ActiveSkill skill)
    {
        if (skill == null) return;
        if (!allActiveSkills.Contains(skill)) allActiveSkills.Add(skill);
        skill.gameObject.SetActive(true);
        Debug.Log($"[Active] {skill.displayName} 획득!");
    }
}