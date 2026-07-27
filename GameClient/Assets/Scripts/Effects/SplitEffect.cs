using UnityEngine;

/// <summary>
/// 분열: 충돌 지점에서 추가 투사체를 부채꼴로 스폰.
/// 자식은 같은 ShotContext를 공유하므로 maxPerShot 카운터가 본체·자식 통합으로 걸린다
/// (분열의 분열로 인한 지수 증식 방지 — maxPerShot은 사실상 필수).
/// </summary>
[CreateAssetMenu(fileName = "FX_Split_", menuName = "Game/Effects/Split")]
public class SplitEffect : EffectDefinition
{
    [Tooltip("추가 생성 개수 (본체 제외)")]
    public int extraProjectiles = 2;
    [Tooltip("갈래 간 각도(도). 진행 방향 기준 좌우 교대로 벌어짐")]
    public float spreadAngleDeg = 25f;
    [Tooltip("발사당 최대 발동 횟수. 지수 증식 방지용 — 0(무제한) 비권장")]
    public int maxPerShot = 1;

    public override void PostDamage(HitContext ctx)
    {
        if (ctx.shot.spawnSubProjectile == null) return;
        if (maxPerShot > 0 && ctx.shot.GetCount(this) >= maxPerShot) return;
        ctx.shot.Increment(this);

        // 기준 방향: 이 충돌의 최종 진행 방향과 동일하게
        Vector3 baseDir = ctx.hasDirectionOverride ? ctx.directionOverride
                        : ctx.suppressReflect ? ctx.hit.inDir
                        : ctx.hit.outDir;
        // 즉시 재충돌 방지를 위해 법선 방향으로 살짝 띄워 스폰
        Vector3 origin = ctx.hit.point + ctx.hit.normal * 0.05f;

        for (int i = 1; i <= extraProjectiles; i++)
        {
            float sign = (i % 2 == 1) ? 1f : -1f;
            int step = (i + 1) / 2;
            Vector3 dir = Quaternion.AngleAxis(sign * step * spreadAngleDeg, Vector3.up) * baseDir;
            ctx.shot.spawnSubProjectile(origin, dir);
        }
    }
}
