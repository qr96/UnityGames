using UnityEngine;

/// <summary>
/// 효과 정의 베이스 (ScriptableObject).
/// 영웅 고유 효과와 룬이 같은 타입을 공유한다 — HeroData.effects에 넣으면 고유 효과,
/// 이후 룬 시스템에서 장착 룬의 EffectDefinition을 같은 리스트에 합류시키면 룬.
///
/// [규칙] SO는 공유 에셋 — 인스턴스 필드에 런타임 상태 저장 금지.
///        발사당 상태는 ctx.shot.GetCount/Increment 사용.
///
/// 훅 순서 (충돌 1회당):
///   trigger.Matches → OnHit (데미지 배율·방향 조작) → 데미지 적용
///   → trigger.Matches 재평가 (killed 확정됨) → PostDamage (폭발·분열·연쇄)
/// </summary>
public abstract class EffectDefinition : ScriptableObject
{
    public TriggerCondition trigger;

    /// <summary>데미지 적용 전. damageMultiplier, suppressReflect 등 조작.</summary>
    public virtual void OnHit(HitContext ctx) { }

    /// <summary>데미지 적용 후. finalDamage/killed 확정 상태. 파생 행동(폭발/분열/연쇄).</summary>
    public virtual void PostDamage(HitContext ctx) { }
}
