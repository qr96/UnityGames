using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 보유 스킬 관리. 뱀서식 — 보유 = 자동 발동.
/// 획득한 스킬은 바로 SkillExecutor가 돌림. 장착/해제 개념 없음.
/// Managers 오브젝트에 부착.
/// </summary>
public class PlayerSkillManager : MonoBehaviour
{
    public static PlayerSkillManager Instance { get; private set; }

    // 상한 — 나중에 밸런스 잡을 때 조절
    public const int ActiveMaxCount = 4;
    public const int PassiveMaxCount = 4;

    // 스킬 → 현재 레벨 (1부터 시작)
    readonly Dictionary<SkillData, int> _skills = new();

    public event Action OnSkillsChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── 조회 ──────────────────────────────────────────────────────────────

    public bool Has(SkillData skill) => skill != null && _skills.ContainsKey(skill);

    public int GetLevel(SkillData skill)
    {
        if (skill == null) return 0;
        return _skills.TryGetValue(skill, out var lv) ? lv : 0;
    }

    public int CountActives() => CountByCategory(SkillCategory.Active);
    public int CountPassives() => CountByCategory(SkillCategory.Passive);

    public bool IsActiveFull() => CountActives() >= ActiveMaxCount;
    public bool IsPassiveFull() => CountPassives() >= PassiveMaxCount;

    public IEnumerable<SkillData> GetActives()
    {
        foreach (var kv in _skills)
            if (kv.Key.category == SkillCategory.Active) yield return kv.Key;
    }

    public IEnumerable<SkillData> GetPassives()
    {
        foreach (var kv in _skills)
            if (kv.Key.category == SkillCategory.Passive) yield return kv.Key;
    }

    public IEnumerable<KeyValuePair<SkillData, int>> GetAll() => _skills;

    public bool CanLevelUp(SkillData skill)
    {
        if (skill == null || !_skills.TryGetValue(skill, out var lv)) return false;
        return lv < skill.maxLevel;
    }

    int CountByCategory(SkillCategory category)
    {
        int count = 0;
        foreach (var kv in _skills)
            if (kv.Key.category == category) count++;
        return count;
    }

    // ── 획득/강화 ────────────────────────────────────────────────────────

    /// <summary>
    /// 신규 스킬 획득 (Lv.1로 추가).
    /// 이미 보유 중이거나 카테고리 상한 초과 시 실패.
    /// </summary>
    public bool TryGainSkill(SkillData skill)
    {
        if (skill == null) return false;

        if (Has(skill))
        {
            Debug.LogWarning($"[PlayerSkill] 이미 보유한 스킬: {skill.skillId}");
            return false;
        }

        if (skill.category == SkillCategory.Active && IsActiveFull())
        {
            Debug.LogWarning("[PlayerSkill] 액티브 상한 초과");
            return false;
        }

        if (skill.category == SkillCategory.Passive && IsPassiveFull())
        {
            Debug.LogWarning("[PlayerSkill] 패시브 상한 초과");
            return false;
        }

        _skills[skill] = 1;
        OnSkillsChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 보유 스킬 1레벨 상승. 미보유 또는 최대 레벨이면 실패.
    /// </summary>
    public bool TryLevelUpSkill(SkillData skill)
    {
        if (skill == null) return false;

        if (!_skills.TryGetValue(skill, out var lv))
        {
            Debug.LogWarning($"[PlayerSkill] 미보유 스킬 레벨업 시도: {skill.skillId}");
            return false;
        }

        if (lv >= skill.maxLevel)
        {
            Debug.LogWarning($"[PlayerSkill] 이미 최대 레벨: {skill.skillId}");
            return false;
        }

        _skills[skill] = lv + 1;
        OnSkillsChanged?.Invoke();
        return true;
    }

    // ── 디버그 / 테스트 ───────────────────────────────────────────────────
    // 레벨업 UI 붙기 전까진 여기서 직접 호출해서 테스트.

    [Header("테스트용")]
    [Tooltip("인스펙터 ContextMenu 또는 외부 스크립트에서 호출해 스킬 추가")]
    public SkillData[] debugSkills;

    [ContextMenu("디버그: debugSkills 전부 획득")]
    void DebugGainAll()
    {
        if (debugSkills == null) return;
        foreach (var s in debugSkills) TryGainSkill(s);
    }

    [ContextMenu("디버그: 보유 스킬 전부 레벨업")]
    void DebugLevelUpAll()
    {
        var keys = new List<SkillData>(_skills.Keys);
        foreach (var s in keys) TryLevelUpSkill(s);
    }

    [ContextMenu("디버그: 전부 초기화")]
    void DebugReset()
    {
        _skills.Clear();
        OnSkillsChanged?.Invoke();
    }
}