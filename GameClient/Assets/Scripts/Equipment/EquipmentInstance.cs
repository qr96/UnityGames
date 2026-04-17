using System;
using System.Collections.Generic;

/// <summary>
/// 장비 런타임 인스턴스. 인벤토리에 실제로 들어가는 개별 아이템.
/// EquipmentData는 원본 공유 데이터, 이 클래스가 개별 아이템 상태를 관리.
/// </summary>
[Serializable]
public class EquipmentInstance
{
    public EquipmentData data;
    public int enhanceLevel;
    public List<EquipmentOption> options = new();

    public EquipmentInstance(EquipmentData data)
    {
        this.data = data;
        enhanceLevel = 0;
    }

    // ── 스탯 ──────────────────────────────────────────────────────────────

    /// <summary>강화 포함 기본 스탯값.</summary>
    public float GetBaseStatValue() => data.GetBaseStatValue(enhanceLevel);

    /// <summary>특정 스탯 합산 (기본 스탯 + 옵션).</summary>
    public float GetTotalStat(StatType statType)
    {
        float total = 0f;

        if (data.baseStat == statType)
            total += GetBaseStatValue();

        foreach (var option in options)
            if (option.statType == statType)
                total += option.value;

        return total;
    }

    /// <summary>
    /// StatModifier id. 슬롯 기준으로 생성해서 같은 슬롯 장비 교체 시
    /// RemoveModifiersById()로 이전 장비 스탯을 정확히 제거할 수 있음.
    /// </summary>
    public string GetModifierId() => $"equip_{data.slot}";
}