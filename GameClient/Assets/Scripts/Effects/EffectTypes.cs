using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 효과/룬 발동 조건. 기획서의 영웅 효과 조건과 룬 발동 조건을 하나로 통합.
///
/// 기획서 매핑:
///   영웅 "벽 or 적에 충돌 시"      → OnWallHit / OnEnemyHit / Always
///   영웅 "연속 사용 시"            → ConsecutiveUseAtLeast (value=2)
///   영웅 "마지막 사용 시"          → OnLastUse
///   룬 "벽에 N번 충돌 시"          → WallBounceAtLeast
///   룬 "적에 N번 충돌 시"          → EnemyHitAtLeast
///   룬 "벽 미충돌 상태로 적 충돌"  → EnemyHitWithoutWall
///   룬 "기력 N 이상/이하"          → EnergyAtLeast / EnergyAtMost (발사 시점 기준)
///   룬 "기력 N 소모 시"            → EnergySpentAtLeast (스테이지 누적, 이번 발사 포함)
///   룬 "적 처치 시"                → OnKill (PostDamage 단계에서만 참)
///   룬 "체력 N% 이상/이하 적 충돌" → TargetHpPctAtLeast / AtMost (피격 전 HP 기준)
/// </summary>
public enum TriggerType
{
    Always,
    OnWallHit,
    OnEnemyHit,
    WallBounceAtLeast,      // value = N. 누적 벽 반사 횟수 (현재 충돌 포함) >= N
    EnemyHitAtLeast,        // value = N. 누적 적 타격 횟수 (현재 충돌 포함) >= N
    EnemyHitWithoutWall,    // 벽 반사 0회 상태에서의 적 충돌
    EnergyAtLeast,          // value = N. 발사 직전 기력 >= N
    EnergyAtMost,
    EnergySpentAtLeast,     // value = N. 이 영웅의 스테이지 누적 기력 소모 >= N
    ConsecutiveUseAtLeast,  // value = N. 같은 영웅 연속 발사 횟수 (이번 포함) >= N
    OnLastUse,              // 이번 발사로 기력 0
    OnKill,                 // 이 충돌로 처치 (PostDamage 단계에서만 참)
    TargetHpPctAtLeast,     // value = 0~100. 피격 전 대상 HP%
    TargetHpPctAtMost,
}

[Serializable]
public struct TriggerCondition
{
    public TriggerType type;
    [Tooltip("N값. 횟수/기력은 정수, HP%는 0~100")]
    public float value;

    /// <summary>현재 충돌 컨텍스트에서 조건 충족 여부. OnHit/PostDamage 양쪽에서 재평가됨.</summary>
    public bool Matches(HitContext ctx)
    {
        switch (type)
        {
            case TriggerType.Always: return true;
            case TriggerType.OnWallHit: return ctx.IsWall;
            case TriggerType.OnEnemyHit: return !ctx.IsWall;
            case TriggerType.WallBounceAtLeast: return ctx.wallBounceCount >= value;
            case TriggerType.EnemyHitAtLeast: return !ctx.IsWall && ctx.enemyHitCount >= value;
            case TriggerType.EnemyHitWithoutWall: return !ctx.IsWall && ctx.wallBounceCount == 0;
            case TriggerType.EnergyAtLeast: return ctx.shot.energyAtFire >= value;
            case TriggerType.EnergyAtMost: return ctx.shot.energyAtFire <= value;
            case TriggerType.EnergySpentAtLeast: return ctx.shot.energySpentTotal >= value;
            case TriggerType.ConsecutiveUseAtLeast: return ctx.shot.consecutiveUses >= value;
            case TriggerType.OnLastUse: return ctx.shot.isLastUse;
            case TriggerType.OnKill: return ctx.killed; // OnHit 시점엔 항상 false
            case TriggerType.TargetHpPctAtLeast: return !ctx.IsWall && ctx.targetHpPct * 100f >= value;
            case TriggerType.TargetHpPctAtMost: return !ctx.IsWall && ctx.targetHpPct * 100f <= value;
            default: return false;
        }
    }
}

/// <summary>
/// 발사 1회의 공유 컨텍스트. 본체와 분열로 생긴 자식 투사체가 같은 인스턴스를 공유한다.
/// - 발사 시점 스냅샷 (기력, 연속 사용 등)
/// - 발사당 효과 카운터 ("발사당 최대 N회" 제한. SO는 공유 에셋이라 상태 보관 금지 → 여기서 관리)
/// - 자식 투사체 스폰 콜백 (BattleManager가 주입)
/// </summary>
public class ShotContext
{
    public HeroData hero;
    public int star;                // 발사 시점 영웅 성급
    public int energyAtFire;        // 발사 직전 기력 (소모 전)
    public int energySpentTotal;    // 이 영웅의 스테이지 누적 기력 소모 (이번 발사 포함)
    public int consecutiveUses;     // 같은 영웅 연속 발사 횟수 (이번 발사 포함, 최소 1)
    public bool isLastUse;          // 이번 발사로 기력 0
    public System.Random rng;
    public Action<Vector3, Vector3> spawnSubProjectile; // (위치, 방향)

    /// <summary>이번 발사에 활성인 효과 (성급 필터 통과분 + 이후 장착 룬 합류).
    /// 발사 시점에 확정 — 투사체는 이 리스트만 본다.</summary>
    public List<EffectDefinition> effects;

    readonly Dictionary<EffectDefinition, int> _counters = new Dictionary<EffectDefinition, int>();
    public int GetCount(EffectDefinition key) => _counters.TryGetValue(key, out var v) ? v : 0;
    public void Increment(EffectDefinition key) => _counters[key] = GetCount(key) + 1;
}

/// <summary>
/// 충돌 1회의 컨텍스트. 효과들이 읽고(상태) 쓰는(결과 조작) 대상.
/// 수명: ResolveHit 한 번. 카운터(wallBounceCount 등)는 현재 충돌을 포함한 누적값.
/// </summary>
public class HitContext
{
    // ---- 상태 (읽기) ----
    public Projectile projectile;
    public ShotContext shot;
    public ReflectionSolver.Hit hit;
    public Enemy enemy;             // null이면 벽
    public bool IsWall => enemy == null;
    public float targetHpPct;       // 피격 전 HP 비율 0~1. 벽이면 -1
    public int wallBounceCount;     // 현재 충돌 포함 누적
    public int enemyHitCount;

    // ---- 결과 (효과가 조작) ----
    public float damageMultiplier = 1f;    // OnHit에서 누적곱
    public int finalDamage;                // PostDamage 시점 유효. 벽이면 0
    public bool killed;                    // PostDamage 시점 유효
    public bool suppressReflect;           // 관통: 반사 취소, 입사 방향 유지
    public bool hasDirectionOverride;      // 연쇄: 방향 강제 (suppressReflect보다 우선)
    public Vector3 directionOverride;
}