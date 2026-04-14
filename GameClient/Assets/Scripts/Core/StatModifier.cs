using System;

/// <summary>
/// 장비/스킬/버프/환생 등이 스탯에 가하는 수정값.
/// PlayerStats가 List<StatModifier>를 보유하고 합산해서 TotalStat을 계산.
///
/// id 규칙:
///   장비 슬롯  →  "slot_weapon", "slot_helmet" ...
///   스킬 패시브 →  "skill_{skillName}"
///   버프        →  "buff_{buffName}"
///   환생        →  "rebirth"
/// </summary>
[Serializable]
public class StatModifier
{
    public string id;
    public StatType statType;
    public float value;
    public ModifierSource source;

    public StatModifier(string id, StatType statType, float value, ModifierSource source)
    {
        this.id = id;
        this.statType = statType;
        this.value = value;
        this.source = source;
    }
}
