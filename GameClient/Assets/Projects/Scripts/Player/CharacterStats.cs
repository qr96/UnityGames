using UnityEngine;

/// <summary>
/// 캐릭터의 모든 수치. 코드에 하드코딩하지 않고 에셋으로 분리한다.
/// Project 창 > Create > Game > Character Stats 로 생성.
/// </summary>
[CreateAssetMenu(fileName = "CharacterStats", menuName = "Game/Character Stats")]
public class CharacterStats : ScriptableObject
{
    [Header("Move")]
    [Tooltip("기본 이동 속도 (m/s)")]
    public float moveSpeed = 4.5f;

    [Tooltip("달리기 속도 (m/s)")]
    public float sprintSpeed = 7.5f;

    [Tooltip("목표 속도까지 도달하는 가속도. 높을수록 즉각적")]
    public float acceleration = 40f;

    [Tooltip("입력이 없을 때 감속도. 높을수록 칼같이 멈춤")]
    public float deceleration = 50f;

    [Tooltip("진행 방향으로 회전하는 데 걸리는 시간(초). 작을수록 민첩")]
    public float rotationSmoothTime = 0.08f;

    [Header("Jump")]
    [Tooltip("지상 점프 최고 높이 (m)")]
    public float jumpHeight = 1.4f;

    [Tooltip("중력 가속도. 음수. -9.81보다 세게 주면 손맛이 좋아진다")]
    public float gravity = -22f;

    [Tooltip("낙하 속도 상한 (m/s)")]
    public float terminalVelocity = -40f;

    [Range(0f, 1f)]
    [Tooltip("공중에서의 조작 가능 비율. 0이면 공중 제어 불가")]
    public float airControl = 0.45f;

    [Tooltip("발판에서 떨어진 뒤에도 점프를 허용하는 시간(초)")]
    public float coyoteTime = 0.12f;

    [Tooltip("착지 직전에 누른 점프를 기억하는 시간(초)")]
    public float jumpBufferTime = 0.12f;

    [Header("Lunge (공중 도약)")]
    [Tooltip("착지 전까지 가능한 도약 횟수")]
    [Min(0)]
    public int lungeCount = 1;

    [Tooltip("도약 속도 (m/s). 이동 속도의 2~3배가 적당하다")]
    public float lungeSpeed = 13f;

    [Tooltip("도약 지속 시간(초). 속도 x 시간 = 도약 거리")]
    public float lungeDuration = 0.28f;

    [Range(0f, 1f)]
    [Tooltip("도약이 끝난 뒤 남기는 속도 비율. 0이면 즉시 멈춤")]
    public float lungeExitSpeedRatio = 0.4f;

    [Tooltip("점프 직후 도약까지의 최소 간격(초). 연타 오작동 방지")]
    public float lungeCooldown = 0.15f;

    [Header("Action")]
    [Tooltip("기본 공격 동작이 캐릭터를 잠그는 시간(초)")]
    public float attackDuration = 0.45f;
}