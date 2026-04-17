using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 런타임 상태 (레벨, XP, 현재HP).
/// 정적 수치는 CharacterGrowthData에서 읽어오고,
/// 장비/스킬/버프 보너스는 StatModifier 리스트로 관리.
/// </summary>
public class PlayerStats : MonoBehaviour, IDamageable
{
    public static PlayerStats Instance { get; private set; }

    [Header("데이터")]
    [SerializeField] CharacterGrowthData _growthData;

    // ── 런타임 상태 ───────────────────────────────────────────────────────
    public int Level { get; private set; } = 1;
    public int CurrentXP { get; private set; } = 0;
    public int CurrentHP { get; private set; }

    // ── 스탯 모디파이어 ───────────────────────────────────────────────────
    readonly List<StatModifier> _modifiers = new();

    // ── 베이스 스탯 (레벨만 반영) ─────────────────────────────────────────
    public int BaseMaxHP => _growthData.GetMaxHP(Level);
    public int BaseAttack => _growthData.GetAttack(Level);
    public int BaseDefense => _growthData.GetDefense(Level);

    // ── 토탈 스탯 (베이스 + 모디파이어 합산) ─────────────────────────────
    public int TotalMaxHP => BaseMaxHP + (int)SumModifiers(StatType.HP);
    public int TotalAttack => BaseAttack + (int)SumModifiers(StatType.Attack);
    public int TotalDefense => BaseDefense + (int)SumModifiers(StatType.Defense);
    public float TotalCritChance => SumModifiers(StatType.CriticalChance); // base 0%
    public float TotalCritDamage => _growthData.baseCritDamage + SumModifiers(StatType.CriticalDamage); // base 150%

    // ── 이벤트 ────────────────────────────────────────────────────────────
    public event Action<int> OnLevelUp;   // 새 레벨
    public event Action<int, int> OnXPChanged; // 현재XP, 필요XP
    public event Action<int, int> OnHPChanged; // 현재HP, 최대HP
    public event Action OnDied;

    // ─────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (_growthData == null)
        {
            Debug.LogError("[PlayerStats] CharacterGrowthData가 할당되지 않았습니다.");
            return;
        }

        CurrentHP = TotalMaxHP;
    }

    // ── 경험치 / 레벨업 ───────────────────────────────────────────────────

    public int GetRequiredXPForCurrentLevel() => _growthData.GetRequiredXP(Level);

    public void AddXP(int xp)
    {
        if (Level >= _growthData.maxLevel) return;

        CurrentXP += xp;
        OnXPChanged?.Invoke(CurrentXP, _growthData.GetRequiredXP(Level));

        while (Level < _growthData.maxLevel)
        {
            int required = _growthData.GetRequiredXP(Level);
            if (required <= 0 || CurrentXP < required) break;
            CurrentXP -= required;
            LevelUp();
        }
    }

    void LevelUp()
    {
        int oldMaxHP = TotalMaxHP;
        Level++;

        // 레벨업으로 늘어난 최대HP만큼 현재HP도 함께 증가
        int hpGain = TotalMaxHP - oldMaxHP;
        CurrentHP = Mathf.Min(CurrentHP + hpGain, TotalMaxHP);

        OnLevelUp?.Invoke(Level);
        OnXPChanged?.Invoke(CurrentXP, _growthData.GetRequiredXP(Level));
        OnHPChanged?.Invoke(CurrentHP, TotalMaxHP);

        Debug.Log($"[PlayerStats] 레벨업 → Lv.{Level} | MaxHP:{TotalMaxHP} ATK:{TotalAttack} DEF:{TotalDefense}");
    }

    // ── IDamageable ───────────────────────────────────────────────────────

    public void TakeDamage(int damage, Vector3 hitDir = default)
    {
        if (CurrentHP <= 0) return;

        int actual = GameFormulas.GetActualDamage(damage, TotalDefense);
        CurrentHP = Mathf.Max(0, CurrentHP - actual);
        OnHPChanged?.Invoke(CurrentHP, TotalMaxHP);

        if (CurrentHP <= 0) Die();
    }

    public void Heal(int amount)
    {
        if (CurrentHP <= 0) return;
        CurrentHP = Mathf.Min(CurrentHP + amount, TotalMaxHP);
        OnHPChanged?.Invoke(CurrentHP, TotalMaxHP);
    }

    void Die()
    {
        Debug.Log("[PlayerStats] 사망");
        OnDied?.Invoke();
        // 마을 귀환은 GameManager에서 OnDied 구독해서 처리
    }

    // ── StatModifier ──────────────────────────────────────────────────────

    public void AddModifier(StatModifier modifier)
    {
        _modifiers.Add(modifier);
        CurrentHP = Mathf.Min(CurrentHP, TotalMaxHP); // 최대HP 변동 시 현재HP 초과 방지
        OnHPChanged?.Invoke(CurrentHP, TotalMaxHP);
    }

    /// <summary>동일 id를 가진 모디파이어 전부 제거. 장비 탈착 시 사용.</summary>
    public void RemoveModifiersById(string id)
    {
        _modifiers.RemoveAll(m => m.id == id);
        CurrentHP = Mathf.Min(CurrentHP, TotalMaxHP);
        OnHPChanged?.Invoke(CurrentHP, TotalMaxHP);
    }

    /// <summary>특정 소스의 모디파이어 전부 제거. 버프 해제 등에 사용.</summary>
    public void RemoveModifiersBySource(ModifierSource source)
    {
        _modifiers.RemoveAll(m => m.source == source);
        CurrentHP = Mathf.Min(CurrentHP, TotalMaxHP);
        OnHPChanged?.Invoke(CurrentHP, TotalMaxHP);
    }

    float SumModifiers(StatType type)
    {
        float sum = 0f;
        foreach (var m in _modifiers)
            if (m.statType == type) sum += m.value;
        return sum;
    }
}
