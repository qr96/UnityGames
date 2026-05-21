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

    /// <summary>
    /// 기본공격 모션 카테고리. 같은 모션이면 같은 애니메이션 클립 재생 + 무기 외형만 다름.
    /// (예: 초보자의 방망이 휘두르기 = 검사의 검 휘두르기 — 둘 다 Swing)
    /// </summary>
    public enum AttackMotion
    {
        Swing,      // 휘두르기 (방망이, 검, 도끼 등)
        Thrust,     // 찌르기 (창, 단검 등)
        Shoot,      // 활쏘기
        Cast,       // 마법 시전 (지팡이)
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