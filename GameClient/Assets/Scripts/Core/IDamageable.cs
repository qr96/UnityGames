using UnityEngine;

/// <summary>
/// 데미지를 받을 수 있는 대상. 잡몹(Enemy), 보스(Boss) 등이 구현.
/// 투사체/근접 공격은 이 인터페이스로 때리므로 대상 종류를 몰라도 됨.
/// </summary>
public interface IDamageable
{
    /// <summary>데미지를 입힌다. hitDirection은 넉백 등에 사용(필요 없으면 무시).</summary>
    void TakeHit(int damage);

    /// <summary>
    /// 즉시 처치. 충돌 즉사 등 '데미지 수치와 무관한 제거'에 사용.
    /// 보스처럼 즉사 면역인 대상은 무시(no-op)로 구현할 수 있다.
    /// (큰 데미지 수치(예: 999)로 즉사를 흉내내면 보스 같은 고체력 대상에
    ///  의도치 않은 실데미지가 들어가므로 반드시 이 메서드를 쓸 것.)
    /// </summary>
    void Kill();

    bool IsDead { get; }

    /// <summary>월드 위치 (타격 이펙트 생성 등에 사용).</summary>
    Vector3 Position { get; }
}