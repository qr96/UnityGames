using System;

[Serializable]
public class EquipmentOption
{
    public StatType statType;
    public float value;

    public EquipmentOption(StatType statType, float value)
    {
        this.statType = statType;
        this.value = value;
    }
}
