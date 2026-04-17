using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 장비 랜덤 옵션 롤링 유틸리티.
/// 장비 획득/구매 시 EquipmentInstance 생성 후 호출.
/// </summary>
public static class EquipmentOptionRoller
{
    // ── 슬롯별 옵션 풀 ────────────────────────────────────────────────────
    static readonly Dictionary<EquipmentSlot, StatType[]> _slotPool = new()
    {
        { EquipmentSlot.Weapon,     new[] { StatType.Attack, StatType.CriticalChance, StatType.CriticalDamage, StatType.AttackSpeed } },
        { EquipmentSlot.Helmet,     new[] { StatType.HP, StatType.Defense, StatType.CriticalChance } },
        { EquipmentSlot.Armor,      new[] { StatType.HP, StatType.Defense, StatType.Attack } },
        { EquipmentSlot.Gloves,     new[] { StatType.Attack, StatType.CriticalChance, StatType.CriticalDamage, StatType.AttackSpeed } },
        { EquipmentSlot.Boots,      new[] { StatType.MoveSpeed, StatType.HP, StatType.Defense } },
        { EquipmentSlot.Accessory1, new[] { StatType.CriticalChance, StatType.CriticalDamage, StatType.Attack, StatType.HP } },
        { EquipmentSlot.Accessory2, new[] { StatType.CriticalDamage, StatType.CriticalChance, StatType.AttackSpeed, StatType.MoveSpeed } },
    };

    // ── 스탯별 수치 범위 ──────────────────────────────────────────────────
    static readonly Dictionary<StatType, (float min, float max)> _statRange = new()
    {
        { StatType.Attack,         (2f,  18f) },
        { StatType.Defense,        (1f,  10f) },
        { StatType.HP,             (10f, 80f) },
        { StatType.CriticalChance, (0.5f, 3.5f) },
        { StatType.CriticalDamage, (3f,  25f) },
        { StatType.MoveSpeed,      (1f,  6f)  },
        { StatType.AttackSpeed,    (1f,  5f)  },
    };

    /// <summary>
    /// instance.options를 새로 롤링.
    /// 장비 획득/구매 시 호출.
    /// </summary>
    public static void Roll(EquipmentInstance instance)
    {
        instance.options.Clear();

        int count = instance.data.GetOptionCount();
        var pool = new List<StatType>(_slotPool[instance.data.slot]);

        // 기본 스탯과 동일한 스탯 제외 (중복 방지)
        pool.Remove(instance.data.baseStat);

        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            int idx = Random.Range(0, pool.Count);
            StatType stat = pool[idx];
            pool.RemoveAt(idx);

            var (min, max) = _statRange[stat];
            float value = Random.Range(min, max);

            instance.options.Add(new EquipmentOption(stat, value));
        }
    }
}