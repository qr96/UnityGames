using System;
using UnityEngine;

/// <summary>
/// 전투 이벤트 버스. 전투 중 일어나는 모든 사건을 여기로 발행한다.
/// 룬("벽에 N번 충돌 시", "적 처치 시"...)과 영웅 고유효과는
/// 이 이벤트들을 구독하는 것만으로 구현된다 → 새 룬 추가 = 데이터 작업.
/// </summary>
public static class BattleEvents
{
    // ---- 투사체 관련 ----
    public static event Action<Projectile> ShotFired;                    // 발사됨
    public static event Action<Projectile, Vector3> WallBounced;         // 벽에 튕김 (위치)
    public static event Action<Projectile, Enemy, int> EnemyHit;         // 적 타격 (데미지)
    public static event Action<Projectile, Enemy> EnemyKilled;           // 적 처치
    public static event Action<Projectile, float> ProjectileLanded;      // 발사 라인 복귀 (착지 x좌표)

    // ---- 턴/스테이지 관련 ----
    public static event Action TurnStarted;    // 조준 가능 상태 진입
    public static event Action TurnEnded;      // 모든 투사체 착지 완료
    public static event Action StageCleared;   // 적 전멸
    public static event Action StageFailed;    // 모든 영웅 기력 소진 & 적 잔존

    public static void RaiseShotFired(Projectile p) => ShotFired?.Invoke(p);
    public static void RaiseWallBounced(Projectile p, Vector3 pos) => WallBounced?.Invoke(p, pos);
    public static void RaiseEnemyHit(Projectile p, Enemy e, int dmg) => EnemyHit?.Invoke(p, e, dmg);
    public static void RaiseEnemyKilled(Projectile p, Enemy e) => EnemyKilled?.Invoke(p, e);
    public static void RaiseProjectileLanded(Projectile p, float x) => ProjectileLanded?.Invoke(p, x);
    public static void RaiseTurnStarted() => TurnStarted?.Invoke();
    public static void RaiseTurnEnded() => TurnEnded?.Invoke();
    public static void RaiseStageCleared() => StageCleared?.Invoke();
    public static void RaiseStageFailed() => StageFailed?.Invoke();

    /// <summary>씬 리로드 시 구독 누수 방지용. 전투 씬 진입 시 호출 권장.</summary>
    public static void ClearAll()
    {
        ShotFired = null; WallBounced = null; EnemyHit = null;
        EnemyKilled = null; ProjectileLanded = null;
        TurnStarted = null; TurnEnded = null;
        StageCleared = null; StageFailed = null;
    }
}
