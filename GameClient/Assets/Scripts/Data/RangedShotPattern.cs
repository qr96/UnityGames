using UnityEngine;

// 원거리 발사(히트스캔). 홀드해서 차지할수록 조준 원뿔이 좁아지고,
// 발사 시 그 원뿔 안에서 무작위 방향으로 한 발 나간다.
// 조준 기준은 사거리 안의 '가장 가까운 대상' — 없으면 정면.
[CreateAssetMenu(fileName = "RangedShotPattern", menuName = "혹한/Attack Pattern/원거리 발사")]
public class RangedShotPattern : AttackPattern
{
    [Header("사거리")]
    public float range = 12f;
    [Tooltip("탄이 대상에 맞았다고 볼 좌우 여유(월드 단위)")]
    public float hitRadius = 0.5f;

    [Header("차지")]
    [Tooltip("완전 차지까지 걸리는 시간(초)")]
    public float chargeSeconds = 1.2f;
    [Tooltip("차지 0일 때 조준 원뿔 각도")]
    public float spreadAtZero = 45f;
    [Tooltip("완전 차지일 때 조준 원뿔 각도")]
    public float spreadAtFull = 3f;

    [Header("위력")]
    [Tooltip("완전 차지 시 위력 배수 (1이면 차지해도 위력은 그대로)")]
    public float fullChargePowerMultiplier = 1f;

    public override bool IsCharged => true;
    public override float ChargeSeconds => Mathf.Max(0.01f, chargeSeconds);

    // 연출용 — 마지막 발사 정보
    [System.NonSerialized] public Vector3 LastShotDirection;
    [System.NonSerialized] public float LastShotDistance;
    [System.NonSerialized] public bool LastShotHit;
    [System.NonSerialized] public float LastShotRange;

    public float SpreadFor(float charge01)
        => Mathf.Lerp(spreadAtZero, spreadAtFull, Mathf.Clamp01(charge01));

    // 조준 기준 방향 = 사거리 안 가장 가까운 대상. 없으면 정면.
    public override IHittable FindPrimary(in AttackContext ctx)
    {
        IHittable best = null;
        float bestSqr = range * range;

        var list = HittableRegistry.All;
        for (int i = 0; i < list.Count; i++)
        {
            IHittable h = list[i];
            if (h == null || !h.CanBeHit || !CanHit(h.Category)) continue;

            Vector3 d = h.HitPosition - ctx.origin; d.y = 0f;
            float sqr = d.sqrMagnitude;
            if (sqr > bestSqr) continue;

            bestSqr = sqr;
            best = h;
        }
        return best;
    }

    public override bool TryGetAimCone(in AttackContext ctx, out Vector3 dir,
                                       out float angleDeg, out float coneRange)
    {
        dir = AimDirection(ctx);
        angleDeg = SpreadFor(ctx.charge01);
        coneRange = range;
        return true;
    }

    private Vector3 AimDirection(in AttackContext ctx)
    {
        IHittable target = FindPrimary(ctx);
        if (target == null) return ctx.forward;

        Vector3 d = target.HitPosition - ctx.origin; d.y = 0f;
        return d.sqrMagnitude > 0.0001f ? d.normalized : ctx.forward;
    }

    public override void Execute(in AttackContext ctx)
    {
        Vector3 aim = AimDirection(ctx);

        // 원뿔 안에서 무작위 방향 선택
        float half = SpreadFor(ctx.charge01) * 0.5f;
        float offset = Random.Range(-half, half);
        Vector3 dir = Quaternion.AngleAxis(offset, Vector3.up) * aim;

        int power = Mathf.Max(1, Mathf.RoundToInt(
            ctx.power * Mathf.Lerp(1f, fullChargePowerMultiplier, Mathf.Clamp01(ctx.charge01))));

        IHittable hit = Raycast(ctx.origin, dir, out float distance);

        LastShotDirection = dir;
        LastShotRange = range;
        LastShotDistance = hit != null ? distance : range;
        LastShotHit = hit != null;

        if (hit != null) hit.ApplyHits(power, ctx.attacker);
    }

    // 콜라이더를 쓰지 않으므로 직선 위 거리로 판정한다
    private IHittable Raycast(Vector3 origin, Vector3 dir, out float distance)
    {
        IHittable best = null;
        float bestForward = range;
        distance = range;

        var list = HittableRegistry.All;
        for (int i = 0; i < list.Count; i++)
        {
            IHittable h = list[i];
            if (h == null || !h.CanBeHit || !CanHit(h.Category)) continue;

            Vector3 d = h.HitPosition - origin; d.y = 0f;

            float forward = Vector3.Dot(d, dir);
            if (forward < 0f || forward > bestForward) continue;

            float lateral = Vector3.Cross(dir, d).magnitude;
            if (lateral > hitRadius) continue;

            bestForward = forward;
            best = h;
        }

        if (best != null) distance = bestForward;
        return best;
    }

    public override void DrawGizmos(Transform t)
    {
        if (t == null) return;
        Gizmos.color = new Color(1f, 0.9f, 0.4f, 0.8f);
        Vector3 f = t.forward; f.y = 0f; f = f.normalized;
        Gizmos.DrawLine(t.position, t.position + f * range);
    }
}