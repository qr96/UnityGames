using UnityEngine;

/// <summary>
/// 직진 이동. 정해진 방향으로 일정 속도. 기본값 -Z (플레이어 쪽).
/// </summary>
public class StraightMover : EnemyMover
{
    [Tooltip("이동 방향. 기본 (0,0,-1) = -Z 방향.")]
    public Vector3 direction = Vector3.back;

    [Tooltip("이동 속도 (초당 미터)")]
    public float speed = 8f;

    public override Vector3 GetVelocity()
    {
        return direction.normalized * speed;
    }
}
