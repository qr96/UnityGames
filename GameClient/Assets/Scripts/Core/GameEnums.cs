public enum StatType
{
    Attack,
    Defense,
    HP,
    CriticalChance,
    CriticalDamage,
    MoveSpeed,
    AttackSpeed
}

public enum ModifierSource
{
    Equipment,
    Skill,
    Buff,
    Rebirth
}

public enum EnemyType
{
    Normal,
    Elite,
    Boss
}

public enum EquipmentTier
{
    Normal = 1,
    Rare = 2,
    Hero = 3,
    Legend = 4
}

public enum EquipmentSlot
{
    Weapon,
    Helmet,
    Armor,
    Gloves,
    Boots,
    Accessory1,
    Accessory2
}

public enum SkillCategory
{
    Active,
    Passive
}

public enum SkillMechanic
{
    Projectile,
    Area,
    Ground,
    Chain,
    Summon,
    SelfBuff,
    Other
}

public enum ActivationType
{
    Cooldown,
    Probability,
    Constant,
    Conditional
}

public enum ProjectileType
{
    Linear, // 발사 시점 방향 고정, 직선 이동
    Homing,  // 타겟 추적, 매 프레임 방향 재계산
    Guaranteed // 타겟에 일정 시간 내에 무조건 도달
}
