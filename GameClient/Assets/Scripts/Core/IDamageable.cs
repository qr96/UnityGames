using UnityEngine;

/// <summary>
/// 데미지를 받을 수 있는 대상. 잡몹(Enemy), 보스(Boss) 등이 구현.
/// 투사체/근접 공격은 이 인터페이스로 때리므로 대상 종류를 몰라도 됨.
/// </summary>
public interface IDamageable
{
    /// <summary>데미지를 입힌다. hitDirection은 넉백 등에 사용(필요 없으면 무시).</summary>
    void TakeHit(int damage);

    bool IsDead { get; }

    /// <summary>월드 위치 (타격 이펙트 생성 등에 사용).</summary>
    Vector3 Position { get; }
}
