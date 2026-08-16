using UnityEngine;

/// <summary>
/// 한 번의 타격에 대한 모든 정보.
/// 구조체로 묶어두면 나중에 넉백, 크리티컬, 속성, 상태이상이 추가되어도
/// IDamageable.TakeDamage의 시그니처를 바꿀 필요가 없다.
/// </summary>
public struct DamageInfo
{
    /// <summary>데미지량</summary>
    public float Amount;

    /// <summary>타격이 발생한 월드 좌표. 이펙트와 사운드 위치에 쓴다</summary>
    public Vector3 Point;

    /// <summary>타격 방향(정규화). 넉백과 히트 리액션 방향에 쓴다</summary>
    public Vector3 Direction;

    /// <summary>넉백 세기 (m/s). 0이면 밀리지 않는다</summary>
    public float Knockback;

    /// <summary>공격한 주체. 어그로 판정이나 아군 사격 방지에 쓴다</summary>
    public GameObject Attacker;

    public DamageInfo(float amount, Vector3 point, Vector3 direction,
                      float knockback = 0f, GameObject attacker = null)
    {
        Amount = amount;
        Point = point;
        Direction = direction;
        Knockback = knockback;
        Attacker = attacker;
    }
}

/// <summary>
/// 데미지를 받을 수 있는 대상.
/// MeleeWeapon은 상대가 적인지 파괴 가능한 상자인지 알 필요가 없다.
/// 이 경계 덕분에 새로운 피격 대상을 추가할 때 공격 코드를 건드리지 않는다.
/// </summary>
public interface IDamageable
{
    /// <summary>현재 살아 있는가. 죽은 대상은 판정에서 제외된다</summary>
    bool IsAlive { get; }

    /// <summary>피해를 적용한다</summary>
    void TakeDamage(in DamageInfo info);
}
