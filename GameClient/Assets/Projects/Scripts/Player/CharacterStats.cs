using UnityEngine;

/// <summary>
/// 캐릭터의 모든 수치. Project 창 > Create > Game > Character Stats 로 생성.
/// </summary>
[CreateAssetMenu(fileName = "CharacterStats", menuName = "Game/Character Stats")]
public class CharacterStats : ScriptableObject
{
    [Header("Move")]
    public float moveSpeed = 4.5f;
    public float sprintSpeed = 7.5f;
    public float acceleration = 40f;
    public float deceleration = 50f;
    [Tooltip("진행 방향으로 회전하는 데 걸리는 시간(초). 작을수록 민첩")]
    public float rotationSmoothTime = 0.08f;

    [Header("Jump")]
    public float jumpHeight = 1.4f;
    [Tooltip("중력 가속도. 음수")]
    public float gravity = -22f;
    public float terminalVelocity = -40f;
    [Range(0f, 1f)] public float airControl = 0.45f;
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.12f;

    [Header("Lunge (공중 도약)")]
    [Min(0)] public int lungeCount = 1;
    public float lungeSpeed = 13f;
    public float lungeDuration = 0.28f;
    [Range(0f, 1f)] public float lungeExitSpeedRatio = 0.4f;
    public float lungeCooldown = 0.15f;

    [Header("Aim (조준)")]
    [Tooltip("조준 중 이동 속도. 느려야 조준의 대가가 생긴다")]
    public float aimMoveSpeed = 2.0f;

    [Tooltip("조준 시작 시 카메라 방향으로 몸을 돌리는 속도 (도/초)")]
    public float aimTurnSpeed = 900f;

    [Tooltip("조준 진입/해제에 걸리는 시간(초). 연타 방지")]
    public float aimEnterTime = 0.1f;

    [Header("Bow (활)")]
    [Tooltip("시위를 완전히 당기는 데 걸리는 시간(초)")]
    public float drawTime = 0.55f;

    [Tooltip("최소 차지에서의 발사 속도 (m/s)")]
    public float minLaunchSpeed = 20f;

    [Tooltip("최대 차지에서의 발사 속도 (m/s)")]
    public float maxLaunchSpeed = 48f;

    [Tooltip("최소 차지 데미지")]
    public float minArrowDamage = 8f;

    [Tooltip("최대 차지 데미지")]
    public float maxArrowDamage = 30f;

    [Tooltip("발사 후 다음 발사까지의 간격(초)")]
    public float fireCooldown = 0.35f;

    [Tooltip("이 차지 미만에서는 발사되지 않는다")]
    [Range(0f, 1f)]
    public float minChargeToFire = 0.15f;
}