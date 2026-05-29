/// <summary>
/// 액티브 스킬이 무엇을 어떻게 만드는지에 대한 태그들.
/// 패시브 스킬은 이 태그를 필터로 사용해 "어떤 액티브 스킬에 효과를 줄지" 결정.
/// </summary>

public enum CreationType
{
    Projectile,  // 투사체 (앞으로 날아감)
    Drop,        // 낙하 (위에서 떨어짐)
    Summon,      // 소환 (정해진 위치에 생성)
}

public enum MovementType
{
    Straight,    // 직진
    Homing,      // 유도
    Bounce,      // 튕김
    Spin,        // 회전
    Stationary,  // 정지
}

public enum CollisionType
{
    Basic,       // 기본 (적 맞으면 데미지)
    Explosion,   // 폭발 (범위 데미지)
    Chain,       // 연쇄 (적 처치 시 인근 적에게 전이)
}

public enum SkillClass
{
    Physical,    // 물리
    Magical,     // 마법
}

public enum Element
{
    None,        // 무속성
    Fire,        // 화염
    Ice,         // 빙결
    Lightning,   // 전격
}

/// <summary>
/// 한 액티브 스킬이 가지는 태그 묶음. 인스펙터에 노출 가능.
/// </summary>
[System.Serializable]
public class SkillTags
{
    public CreationType creation = CreationType.Projectile;
    public MovementType movement = MovementType.Straight;
    public CollisionType collision = CollisionType.Basic;
    public SkillClass skillClass = SkillClass.Physical;
    public Element element = Element.None;
}
