using UnityEngine;

// 반경형: 앞쪽 부채꼴 안의 대상을 친다. 대표 대상 외에 주변도 함께 맞음(도끼·검).
[CreateAssetMenu(fileName = "RadialSwingPattern", menuName = "혹한/Attack Pattern/반경형 휘두름")]
public class RadialSwingPattern : AttackPattern
{
    [Header("범위")]
    public float radius = 3f;
    [Range(10f, 360f)] public float facingAngle = 120f;

    [Header("번짐")]
    [Tooltip("대표 대상 외에 함께 맞는 최대 개수")]
    public int maxExtraTargets = 2;
    [Range(0f, 1f)]
    [Tooltip("주변 대상이 받는 타격 비율")]
    public float othersPowerRatio = 1f;

    public override IHittable FindPrimary(in AttackContext ctx)
    {
        IHittable best = null;
        float bestSqr = radius * radius;
        float cosLimit = Mathf.Cos(facingAngle * 0.5f * Mathf.Deg2Rad);

        var list = HittableRegistry.All;
        for (int i = 0; i < list.Count; i++)
        {
            IHittable h = list[i];
            if (h == null || !h.CanBeHit || !CanHit(h.Category)) continue;

            Vector3 d = h.HitPosition - ctx.origin; d.y = 0f;
            float sqr = d.sqrMagnitude;
            if (sqr > bestSqr) continue;
            if (sqr > 0.0001f && Vector3.Dot(ctx.forward, d.normalized) < cosLimit) continue;

            bestSqr = sqr;
            best = h;
        }
        return best;
    }

    public override void Execute(in AttackContext ctx)
    {
        IHittable primary = FindPrimary(ctx);
        if (primary == null) return; // 허공 휘두름 — 동작만 (연출은 실행기가 재생)

        int targetHits = Mathf.Max(1, ctx.power);
        primary.ApplyHits(targetHits, ctx.attacker);

        if (maxExtraTargets <= 0 || othersPowerRatio <= 0f) return;

        int otherHits = Mathf.Max(1, Mathf.RoundToInt(targetHits * Mathf.Clamp01(othersPowerRatio)));
        float r2 = radius * radius;
        float cosLimit = Mathf.Cos(facingAngle * 0.5f * Mathf.Deg2Rad);
        HitCategory category = primary.Category;

        var used = new System.Collections.Generic.HashSet<IHittable> { primary };
        int applied = 0;

        while (applied < maxExtraTargets)
        {
            IHittable best = null;
            float bestSqr = r2;

            var list = HittableRegistry.All;
            for (int i = 0; i < list.Count; i++)
            {
                IHittable h = list[i];
                if (h == null || used.Contains(h)) continue;
                if (!h.CanBeHit || h.Category != category) continue;

                Vector3 d = h.HitPosition - ctx.origin; d.y = 0f;
                float sqr = d.sqrMagnitude;
                if (sqr > bestSqr) continue;
                if (sqr > 0.0001f && Vector3.Dot(ctx.forward, d.normalized) < cosLimit) continue;

                bestSqr = sqr;
                best = h;
            }

            if (best == null) break;

            used.Add(best);
            best.ApplyHits(otherHits, ctx.attacker);
            applied++;
        }
    }

    public override void DrawGizmos(Transform t)
    {
        if (t == null) return;
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.8f);
        const int seg = 32;
        Vector3 prev = t.position + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= seg; i++)
        {
            float a = (i / (float)seg) * Mathf.PI * 2f;
            Vector3 p = t.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
    }
}
