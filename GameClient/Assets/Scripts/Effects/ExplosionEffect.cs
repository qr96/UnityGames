using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 폭발: 충돌 지점 반경 내 적들에게 범위 데미지.
/// 폭발 데미지 = (적 충돌이면 타격 데미지, 벽 충돌이면 영웅 공격력) × damageRatio.
/// 직접 타격한 적은 범위에서 제외 (이미 본 데미지를 받음).
/// 범위 킬도 BattleEvents로 발행 → 스테이지 잔여 카운트/피드백에 정상 반영.
/// </summary>
[CreateAssetMenu(fileName = "FX_Explosion_", menuName = "Game/Effects/Explosion")]
public class ExplosionEffect : EffectDefinition
{
    public float radius = 1.5f;
    [Tooltip("폭발 데미지 = 기준 데미지 × 이 비율")]
    public float damageRatio = 0.5f;
    [Tooltip("발사당 최대 발동 횟수. 0 = 무제한")]
    public int maxPerShot = 0;

    static readonly Collider[] _overlapBuf = new Collider[32];
    static readonly HashSet<Enemy> _seen = new HashSet<Enemy>();

    public override void PostDamage(HitContext ctx)
    {
        if (maxPerShot > 0 && ctx.shot.GetCount(this) >= maxPerShot) return;
        ctx.shot.Increment(this);

        int baseDmg = ctx.IsWall ? ctx.shot.hero.attack : ctx.finalDamage;
        int aoeDmg = Mathf.Max(1, Mathf.RoundToInt(baseDmg * damageRatio));

        int count = Physics.OverlapSphereNonAlloc(
            ctx.hit.point, radius, _overlapBuf,
            ctx.projectile.collisionMask, QueryTriggerInteraction.Ignore);

        _seen.Clear();
        for (int i = 0; i < count; i++)
        {
            var e = _overlapBuf[i].GetComponentInParent<Enemy>();
            if (e == null || e.IsDead || e == ctx.enemy) continue;
            if (!_seen.Add(e)) continue; // 콜라이더 여러 개인 적 중복 방지

            bool killed = e.TakeDamage(aoeDmg, false);
            BattleEvents.RaiseEnemyHit(ctx.projectile, e, aoeDmg);
            if (killed) BattleEvents.RaiseEnemyKilled(ctx.projectile, e);
        }
        // TODO(연출): 폭발 VFX 훅 — WallBounced처럼 전용 이벤트 추가 예정
    }
}
