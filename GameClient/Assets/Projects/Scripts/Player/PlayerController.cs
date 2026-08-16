using UnityEngine;

public enum PlayerState
{
    Locomotion,  // 지상 이동
    Airborne,    // 공중 (점프 / 낙하)
    Lunge,       // 공중 도약 (한조 도약류)
    Attack       // 액션 잠금
}

/// <summary>
/// 3인칭 백뷰 캐릭터 컨트롤러.
/// 공중에서 스페이스를 한 번 더 누르면 수직 점프가 아니라 수평 도약(Lunge)이 나간다.
/// 도약 중에는 중력이 꺼지고 속도가 고정되어, 짧고 직선적인 돌진이 된다.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputReader))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] CharacterStats stats;
    [SerializeField] PlayerInputReader input;
    [Tooltip("비워두면 Camera.main을 자동으로 잡는다")]
    [SerializeField] Transform cameraTransform;
    [Tooltip("아직 없으면 비워둬도 된다")]
    [SerializeField] Animator animator;

    CharacterController cc;

    PlayerState state = PlayerState.Locomotion;
    float stateTimer;

    Vector3 horizontalVelocity;
    float verticalVelocity;
    float turnSmoothVelocity;

    float coyoteTimer;
    float jumpBufferTimer;

    // 도약
    int lungesUsed;
    float lungeCooldownTimer;
    Vector3 lungeDirection;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int GroundedHash = Animator.StringToHash("Grounded");
    static readonly int AttackHash = Animator.StringToHash("Attack");
    static readonly int LungeHash = Animator.StringToHash("Lunge");

    public PlayerState State => state;
    public bool IsGrounded => cc.isGrounded;
    public int LungesRemaining => stats ? stats.lungeCount - lungesUsed : 0;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (!input) input = GetComponent<PlayerInputReader>();
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;

        if (!stats)
            Debug.LogError($"{name}: CharacterStats가 비어 있다. 인스펙터에 에셋을 꽂아라.", this);
    }

    void Update()
    {
        if (!stats) return;
        float dt = Time.deltaTime;

        TickTimers(dt);

        switch (state)
        {
            case PlayerState.Locomotion: TickLocomotion(dt); break;
            case PlayerState.Airborne: TickAirborne(dt); break;
            case PlayerState.Lunge: TickLunge(dt); break;
            case PlayerState.Attack: TickAttack(dt); break;
        }

        ApplyGravity(dt);

        Vector3 motion = horizontalVelocity + Vector3.up * verticalVelocity;
        cc.Move(motion * dt);

        UpdateAnimator();
    }

    // ── 타이머 ─────────────────────────────────

    void TickTimers(float dt)
    {
        if (input.ConsumeJump()) jumpBufferTimer = stats.jumpBufferTime;
        jumpBufferTimer -= dt;

        if (cc.isGrounded)
        {
            coyoteTimer = stats.coyoteTime;
            lungesUsed = 0;                // 착지하면 도약 회복
        }
        else
        {
            coyoteTimer -= dt;
        }

        lungeCooldownTimer -= dt;
        stateTimer -= dt;
    }

    // ── 상태별 로직 ─────────────────────────────

    void TickLocomotion(float dt)
    {
        if (input.ConsumeAttack()) { TransitionTo(PlayerState.Attack); return; }

        if (jumpBufferTimer > 0f && coyoteTimer > 0f) { DoGroundJump(); return; }

        if (!cc.isGrounded && coyoteTimer <= 0f) { TransitionTo(PlayerState.Airborne); return; }

        MoveHorizontal(dt, 1f);
    }

    void TickAirborne(float dt)
    {
        if (cc.isGrounded && verticalVelocity <= 0f)
        {
            TransitionTo(PlayerState.Locomotion);
            return;
        }

        if (jumpBufferTimer > 0f
            && lungeCooldownTimer <= 0f
            && lungesUsed < stats.lungeCount)
        {
            StartLunge();
            return;
        }

        MoveHorizontal(dt, stats.airControl);
    }

    void TickLunge(float dt)
    {
        // 속도 고정 — 가감속 없이 직선으로 뻗는다
        horizontalVelocity = lungeDirection * stats.lungeSpeed;
        verticalVelocity = 0f;

        // 벽에 박히거나 시간이 다 되면 종료
        bool blocked = (cc.collisionFlags & CollisionFlags.Sides) != 0;

        if (stateTimer <= 0f || blocked)
            TransitionTo(cc.isGrounded ? PlayerState.Locomotion : PlayerState.Airborne);
    }

    void TickAttack(float dt)
    {
        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity, Vector3.zero, stats.deceleration * dt);

        if (stateTimer <= 0f)
            TransitionTo(cc.isGrounded ? PlayerState.Locomotion : PlayerState.Airborne);
    }

    // ── 상태 전이 ──────────────────────────────

    void TransitionTo(PlayerState next)
    {
        if (state == next) return;

        // Exit
        if (state == PlayerState.Lunge)
        {
            // 도약이 끝나면 속도를 조금 죽여서 관성이 과하게 남지 않게 한다
            horizontalVelocity *= stats.lungeExitSpeedRatio;
        }

        state = next;

        // Enter
        switch (state)
        {
            case PlayerState.Attack:
                stateTimer = stats.attackDuration;
                if (animator) animator.SetTrigger(AttackHash);
                break;

            case PlayerState.Lunge:
                stateTimer = stats.lungeDuration;
                if (animator) animator.SetTrigger(LungeHash);
                break;
        }
    }

    // ── 이동 ───────────────────────────────────

    void MoveHorizontal(float dt, float control)
    {
        Vector3 dir = CameraRelativeInput();
        bool hasInput = dir.sqrMagnitude > 0.001f;

        float targetSpeed = hasInput
            ? (input.Sprint ? stats.sprintSpeed : stats.moveSpeed)
            : 0f;

        Vector3 targetVelocity = dir * targetSpeed;
        float rate = (hasInput ? stats.acceleration : stats.deceleration) * control;

        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity, targetVelocity, rate * dt);

        if (hasInput && control > 0f)
            RotateTowards(dir, dt);
    }

    Vector3 CameraRelativeInput()
    {
        Vector2 m = input.Move;
        if (m.sqrMagnitude < 0.01f || !cameraTransform) return Vector3.zero;

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0f; right.y = 0f;
        forward.Normalize(); right.Normalize();

        return Vector3.ClampMagnitude(forward * m.y + right * m.x, 1f);
    }

    void RotateTowards(Vector3 dir, float dt)
    {
        float target = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        float angle = Mathf.SmoothDampAngle(
            transform.eulerAngles.y, target,
            ref turnSmoothVelocity, stats.rotationSmoothTime);

        transform.rotation = Quaternion.Euler(0f, angle, 0f);
    }

    // ── 중력 / 점프 / 도약 ─────────────────────

    void ApplyGravity(float dt)
    {
        if (state == PlayerState.Lunge) return;   // 도약 중에는 중력 없음

        if (cc.isGrounded && state != PlayerState.Airborne && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
            return;
        }

        verticalVelocity += stats.gravity * dt;
        verticalVelocity = Mathf.Max(verticalVelocity, stats.terminalVelocity);
    }

    void DoGroundJump()
    {
        verticalVelocity = Mathf.Sqrt(2f * Mathf.Abs(stats.gravity) * stats.jumpHeight);
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
        lungeCooldownTimer = stats.lungeCooldown;  // 점프 직후 즉시 도약 방지
        TransitionTo(PlayerState.Airborne);
    }

    void StartLunge()
    {
        // 방향 결정: 입력 우선, 없으면 캐릭터 정면
        Vector3 dir = CameraRelativeInput();
        if (dir.sqrMagnitude <= 0.001f) dir = transform.forward;

        dir.y = 0f;
        lungeDirection = dir.normalized;

        // 도약 방향으로 즉시 몸을 돌린다
        transform.rotation = Quaternion.LookRotation(lungeDirection, Vector3.up);
        turnSmoothVelocity = 0f;

        lungesUsed++;
        jumpBufferTimer = 0f;
        lungeCooldownTimer = stats.lungeCooldown;

        TransitionTo(PlayerState.Lunge);
    }

    // ── 표현 계층 ──────────────────────────────

    void UpdateAnimator()
    {
        if (!animator) return;
        animator.SetFloat(SpeedHash, horizontalVelocity.magnitude, 0.1f, Time.deltaTime);
        animator.SetBool(GroundedHash, cc.isGrounded);
    }
}