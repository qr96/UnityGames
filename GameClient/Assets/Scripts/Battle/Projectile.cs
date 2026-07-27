using UnityEngine;

/// <summary>
/// 실제 투사체 (3D). 매 FixedUpdate마다 StepOnce로 다음 충돌까지 전진하고,
/// 충돌마다 효과 파이프라인(ResolveHit)을 거쳐 다음 진행 방향을 결정한다.
///   충돌 → trigger.Matches → OnHit(배율/관통) → 데미지 적용
///        → trigger 재평가(killed 확정) → PostDamage(폭발/분열/연쇄) → 방향 결정
/// </summary>
public class Projectile : MonoBehaviour
{
    [Header("설정 (BattleManager가 주입)")]
    public float speed = 18f;
    public float radius = 0.15f;
    public LayerMask collisionMask;   // Wall | Enemy
    public float launchLineZ;         // 이 z보다 뒤로 후퇴하면 착지

    // 룬/효과 판정용 누적 카운터 (이 투사체 개별. 발사 공유 상태는 ShotContext)
    [HideInInspector] public int wallBounceCount;
    [HideInInspector] public int enemyHitCount;
    [HideInInspector] public HeroData owner;

    Vector3 _dir;
    bool _active;
    ShotContext _shot;

    /// <summary>발사. BattleManager.SpawnProjectile에서 호출.</summary>
    public void Launch(Vector3 origin, Vector3 dir, ShotContext shot)
    {
        transform.position = origin;
        _dir = dir; // Solver가 평면 투영+정규화 처리
        _shot = shot;
        owner = shot.hero;
        speed = owner.projectileSpeed;
        radius = owner.projectileRadius;
        wallBounceCount = 0;
        enemyHitCount = 0;
        _active = true;
        BattleEvents.RaiseShotFired(this);
    }

    void FixedUpdate()
    {
        if (!_active) return;

        Vector3 pos = transform.position;
        float remaining = speed * Time.fixedDeltaTime;
        int guard = 16; // 프레임당 충돌 처리 상한 (안전장치)

        while (_active && remaining > 1e-5f && guard-- > 0)
        {
            // 착지 검사: 후퇴 중(-z)이고 남은 이동거리 안에서 발사 라인을 통과하는가
            if (_dir.z < 0f)
            {
                float travelToLine = pos.z > launchLineZ
                    ? (pos.z - launchLineZ) / -_dir.z
                    : 0f;
                if (travelToLine <= remaining)
                {
                    pos += _dir * travelToLine;
                    transform.position = pos;
                    Land(pos.x);
                    return;
                }
            }

            bool hitSomething = ReflectionSolver.StepOnce(
                ref pos, _dir, radius, remaining, collisionMask,
                out ReflectionSolver.Hit hit, out float consumed);
            remaining -= consumed;

            if (!hitSomething) break;

            Vector3 prevPos = pos;
            _dir = ResolveHit(hit, ref pos);          // 효과 파이프라인이 방향(+관통 시 위치) 결정
            remaining -= (pos - prevPos).magnitude;   // 관통 통과 거리 차감
        }

        transform.position = pos;
    }

    /// <summary>충돌 1회 처리: 효과 파이프라인 실행 후 다음 진행 방향 반환.
    /// 관통으로 콜라이더를 통과할 경우 pos를 출구 지점으로 이동시킨다.</summary>
    Vector3 ResolveHit(ReflectionSolver.Hit hit, ref Vector3 pos)
    {
        var enemy = hit.collider.GetComponentInParent<Enemy>();
        if (enemy != null) enemyHitCount++; else wallBounceCount++;

        var ctx = new HitContext
        {
            projectile = this,
            shot = _shot,
            hit = hit,
            enemy = enemy,
            targetHpPct = enemy != null ? (float)enemy.CurrentHp / enemy.maxHp : -1f,
            wallBounceCount = wallBounceCount,
            enemyHitCount = enemyHitCount,
        };

        var effects = _shot.effects; // 발사 시점에 확정된 활성 효과 (성급 필터 + 이후 룬 합류분)

        // 1) OnHit: 데미지 배율, 관통 등 (데미지 적용 전)
        if (effects != null)
            for (int i = 0; i < effects.Count; i++)
                if (effects[i] != null && effects[i].trigger.Matches(ctx))
                    effects[i].OnHit(ctx);

        // 2) 데미지 적용
        if (enemy != null)
        {
            int rolled = owner.RollDamage(_shot.rng, out bool crit);
            ctx.finalDamage = Mathf.Max(1, Mathf.RoundToInt(rolled * ctx.damageMultiplier));
            ctx.killed = enemy.TakeDamage(ctx.finalDamage, crit);
            BattleEvents.RaiseEnemyHit(this, enemy, ctx.finalDamage);
            if (ctx.killed) BattleEvents.RaiseEnemyKilled(this, enemy);
        }
        else
        {
            BattleEvents.RaiseWallBounced(this, hit.point);
        }

        // 3) PostDamage: killed 확정 상태에서 트리거 재평가 (OnKill이 여기서 참이 됨)
        if (effects != null)
            for (int i = 0; i < effects.Count; i++)
                if (effects[i] != null && effects[i].trigger.Matches(ctx))
                    effects[i].PostDamage(ctx);

        // 4) 다음 방향 결정: 연쇄 > 관통 > 기본 반사
        if (ctx.hasDirectionOverride) return ctx.directionOverride;
        if (ctx.suppressReflect)
        {
            // 관통 통과: 적이 생존했다면 콜라이더 반대편 출구로 위치 이동.
            // (StepOnce가 입사면 바깥에 세워두므로, 그대로 두면 다음 캐스트가
            //  같은 적을 0거리에서 재타격 → 통과 중 다단히트 버그)
            // 적이 죽었다면 콜라이더가 즉시 비활성화되므로 이동 불필요.
            if (enemy != null && !ctx.killed)
                pos = ReflectionSolver.ComputePierceExit(hit.collider, hit.point, hit.inDir, radius);
            return hit.inDir;
        }
        return hit.outDir;
    }

    void Land(float x)
    {
        _active = false;
        BattleEvents.RaiseProjectileLanded(this, x);
        Destroy(gameObject); // 이후 오브젝트 풀로 교체
    }
}