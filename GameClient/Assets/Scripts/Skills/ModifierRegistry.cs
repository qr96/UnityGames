using System.Collections.Generic;
using UnityEngine;

public enum ModifierType
{
    DamageMultiplier,        // 데미지 곱
    CooldownMultiplier,      // 쿨다운 곱 (0.9 = 10% 빨라짐)
    SizeMultiplier,          // 크기 곱
    LifeTimeMultiplier,      // 지속시간 곱
    PierceBonus,             // 관통 +N (덧셈)
}

/// <summary>
/// 패시브 스킬이 등록한 모디파이어들의 중앙 저장소.
/// 액티브 스킬이 발사 시점에 자기 태그로 조회해서 최종 값 계산.
/// </summary>
public class ModifierRegistry : MonoBehaviour
{
    public static ModifierRegistry Instance { get; private set; }

    private struct Entry
    {
        public SkillTagFilter filter;
        public ModifierType type;
        public float value;
    }

    private readonly List<Entry> entries = new List<Entry>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Register(SkillTagFilter filter, ModifierType type, float value)
    {
        entries.Add(new Entry { filter = filter, type = type, value = value });
    }

    /// <summary>곱셈 누적 (기본값 1f).</summary>
    public float GetMultiplier(SkillTags tags, ModifierType type)
    {
        float result = 1f;
        foreach (var e in entries)
        {
            if (e.type != type) continue;
            if (e.filter == null || e.filter.Matches(tags)) result *= e.value;
        }
        return result;
    }

    /// <summary>덧셈 누적 (기본값 0).</summary>
    public int GetBonusInt(SkillTags tags, ModifierType type)
    {
        int result = 0;
        foreach (var e in entries)
        {
            if (e.type != type) continue;
            if (e.filter == null || e.filter.Matches(tags)) result += Mathf.RoundToInt(e.value);
        }
        return result;
    }

    /// <summary>덧셈 누적 (float).</summary>
    public float GetBonusFloat(SkillTags tags, ModifierType type)
    {
        float result = 0f;
        foreach (var e in entries)
        {
            if (e.type != type) continue;
            if (e.filter == null || e.filter.Matches(tags)) result += e.value;
        }
        return result;
    }

    public void Clear() => entries.Clear();
}
