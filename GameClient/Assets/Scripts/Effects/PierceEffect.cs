using UnityEngine;

/// <summary>
/// 관통: 적 충돌 시 반사하지 않고 입사 방향 그대로 통과.
/// 벽에는 적용되지 않음 (벽 관통 = 필드 이탈이므로 항상 반사).
/// </summary>
[CreateAssetMenu(fileName = "FX_Pierce_", menuName = "Game/Effects/Pierce")]
public class PierceEffect : EffectDefinition
{
    [Tooltip("발사당 최대 관통 횟수. 0 = 무제한")]
    public int maxPerShot = 0;

    public override void OnHit(HitContext ctx)
    {
        if (ctx.IsWall) return;
        if (maxPerShot > 0 && ctx.shot.GetCount(this) >= maxPerShot) return;

        ctx.shot.Increment(this);
        ctx.suppressReflect = true;
    }
}
