using UnityEngine;

// 직선형: 바라보는 방향으로 뻗은 띠 안의 대상을 친다(창·사격 판정 등).
// 관통 수만큼 앞에서부터 순서대로 맞는다.
[CreateAssetMenu(fileName = "LineAttackPattern", menuName = "혹한/Attack Pattern/직선형")]
public class LineAttackPattern : AttackPattern
{
    [Header("범위")]
    public float range = 8f;
    [Tooltip("직선 좌우 폭의 절반")]
    public float halfWidth = 0.6f;

    [Header("관통")]
    [Tooltip("맞힐 수 있는 최대 개수(앞에서부터)")]
    public int maxTargets = 1;
    [Range(0f, 1f)]
    [Tooltip("두 번째 이후 대상이 받는 타격 비율")]
    public float piercePowerRatio = 0.5f;

    public override IHittable FindPrimary(in AttackContext ctx)
    {
        IHittable best = null;
        float bestForward = float.MaxValue;

        var list = HittableRegistry.All;
        for (int i = 0; i < list.Count; i++)
        {
            IHittable h = list[i];
            if (h == null || !h.CanBeHit || !CanHit(h.Category)) continue;
            if (!InLine(ctx, h, out float forwardDist)) continue;
            if (forwardDist < bestForward) { bestForward = forwardDist; best = h; }
        }
        return best;
    }

    public override void Execute(in AttackContext ctx)
    {
        int hitCount = 0;
        int power = Mathf.Max(1, ctx.power);

        var used = new System.Collections.Generic.HashSet<IHittable>();

        while (hitCount < Mathf.Max(1, maxTargets))
        {
            IHittable best = null;
            float bestForward = float.MaxValue;

            var list = HittableRegistry.All;
            for (int i = 0; i < list.Count; i++)
            {
                IHittable h = list[i];
                if (h == null || used.Contains(h)) continue;
                if (!h.CanBeHit || !CanHit(h.Category)) continue;
                if (!InLine(ctx, h, out float forwardDist)) continue;
                if (forwardDist < bestForward) { bestForward = forwardDist; best = h; }
            }

            if (best == null) break;

            int hits = hitCount == 0
                ? power
                : Mathf.Max(1, Mathf.RoundToInt(power * Mathf.Clamp01(piercePowerRatio)));

            best.ApplyHits(hits, ctx.attacker);
            used.Add(best);
            hitCount++;
        }
    }

    // 직선 띠 안에 있는지 + 앞쪽 거리
    private bool InLine(in AttackContext ctx, IHittable h, out float forwardDist)
    {
        Vector3 d = h.HitPosition - ctx.origin; d.y = 0f;
        forwardDist = Vector3.Dot(d, ctx.forward);
        if (forwardDist < 0f || forwardDist > range) return false;

        float lateral = Vector3.Cross(ctx.forward, d).magnitude; // 축에서 벗어난 거리
        return lateral <= halfWidth;
    }

    public override void DrawGizmos(Transform t)
    {
        if (t == null) return;
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.8f);
        Vector3 f = t.forward; f.y = 0f; f = f.normalized;
        Vector3 right = Vector3.Cross(Vector3.up, f) * halfWidth;
        Vector3 a = t.position + right, b = t.position - right;
        Gizmos.DrawLine(a, a + f * range);
        Gizmos.DrawLine(b, b + f * range);
        Gizmos.DrawLine(a + f * range, b + f * range);
    }
}
