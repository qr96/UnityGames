using UnityEngine;

/// <summary>
/// 연쇄: 충돌 후 진행 방향을 반경 내 가장 가까운 다른 적 쪽으로 강제.
/// 직접 타격한 적은 대상에서 제외 (같은 적 재타격 루프 방지).
/// </summary>
[CreateAssetMenu(fileName = "FX_Chain_", menuName = "Game/Effects/Chain")]
public class ChainEffect : EffectDefinition
{
    [Tooltip("대상 탐색 반경")]
    public float searchRadius = 6f;
    [Tooltip("발사당 최대 연쇄 횟수. 0 = 무제한")]
    public int maxPerShot = 3;

    static readonly Collider[] _overlapBuf = new Collider[32];

    public override void PostDamage(HitContext ctx)
    {
        if (maxPerShot > 0 && ctx.shot.GetCount(this) >= maxPerShot) return;

        int count = Physics.OverlapSphereNonAlloc(
            ctx.hit.point, searchRadius, _overlapBuf,
            ctx.projectile.collisionMask, QueryTriggerInteraction.Ignore);

        Enemy nearest = null;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            var e = _overlapBuf[i].GetComponentInParent<Enemy>();
            if (e == null || e.IsDead || e == ctx.enemy) continue;

            float sqr = (e.transform.position - ctx.hit.point).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; nearest = e; }
        }
        if (nearest == null) return; // 대상 없으면 발동 안 함 (카운터 미소모)

        ctx.shot.Increment(this);
        ctx.hasDirectionOverride = true;
        ctx.directionOverride = nearest.transform.position - ctx.hit.point; // Solver가 평면 투영
    }
}
