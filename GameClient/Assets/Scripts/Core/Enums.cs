namespace AutoBattler.Core
{
    public enum WeaponType
    {
        Sword,   // 검 - 근거리
        Bow,     // 활 - 원거리
        Staff    // 지팡이 - 원거리/마법
    }

    public enum Team
    {
        Ally,
        Enemy
    }

    public enum SkillTargetType
    {
        SingleEnemy,
        AreaEnemy,
        Self,
        AllyLowestHP
    }

    public enum RewardType
    {
        Weapon,
        Equipment,
        Skill
    }

    public enum EquipmentSlot
    {
        Weapon,
        Armor,
        Accessory
    }

    public enum BattleState
    {
        Idle,
        Preparing,
        Running,
        Won,
        Lost
    }
}
