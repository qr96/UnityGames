using UnityEngine;

public enum PlayerState
{
    Locomotion,  // 지상 이동
    Airborne,    // 공중 (점프 / 낙하)
    Lunge,       // 공중 도약
    Aim          // 활 조준
}

/// <summary>
/// 3인칭 백뷰 캐릭터 컨트롤러 (궁수).
///
/// 조작:
///   좌클릭 홀드 → 조준 진입 + 시위 당김 (누르고 있는 동안에만 차지)
///   좌클릭 뗌   → 발사 (조준 유지)
///   우클릭      → 조준 해제, 3인칭 복귀
///   스페이스    → 점프 (조준 중에도 가능, 조준 유지)
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
    [Tooltip("비워두면 같은 오브젝트에서 자동으로 찾는다")]
    [SerializeField] Bow bow;
    [Tooltip("조준 카메라 전환기")]
    [SerializeField] AimCameraRig cameraRig;
    [SerializeField] Animator animator;

    CharacterController cc;

    PlayerState state = PlayerState.Locomotion;
    float stateTimer;

    Vector3 horizontalVelocity;
    float verticalVelocity;
    float turnSmoothVelocity;

    float coyoteTimer;
    float jumpBufferTimer;

    int lungesUsed;
    float lungeCooldownTimer;
    Vector3 lungeDirection;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int GroundedHash = Animator.StringToHash("Grounded");
    static readonly int AimHash = Animator.StringToHash("Aiming");
    static readonly int FireHash = Animator.StringToHash("Fire");
    static readonly int LungeHash = Animator.StringToHash("Lunge");

    public PlayerState State => state;
    public bool IsGrounded => cc.isGrounded;
    public bool IsAiming => state == PlayerState.Aim;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (!input) input = GetComponent<PlayerInputReader>();
        if (!bow) bow = GetComponent<Bow>();
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;

        if (!stats) Debug.LogError($"{name}: CharacterStats가 비어 있다.", this);
        if (!cameraRig) Debug.LogWarning($"{name}: AimCameraRig가 없다. 카메라가 전환되지 않는다.", this);
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
            case PlayerState.Aim: TickAim(dt); break;
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
            lungesUsed = 0;
        }
        else coyoteTimer -= dt;

        lungeCooldownTimer -= dt;
        stateTimer -= dt;
    }

    // ── 상태별 로직 ─────────────────────────────

    void TickLocomotion(float dt)
    {
        if (input.AttackHeld) { TransitionTo(PlayerState.Aim); return; }

        if (jumpBufferTimer > 0f && coyoteTimer > 0f) { DoGroundJump(); return; }

        if (!cc.isGrounded && coyoteTimer <= 0f) { TransitionTo(PlayerState.Airborne); return; }

        MoveHorizontal(dt, 1f);
    }

    void TickAirborne(float dt)
    {
        if (input.AttackHeld) { TransitionTo(PlayerState.Aim); return; }

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
        horizontalVelocity = lungeDirection * stats.lungeSpeed;
        verticalVelocity = 0f;

        bool blocked = (cc.collisionFlags & CollisionFlags.Sides) != 0;

        if (stateTimer <= 0f || blocked)
            TransitionTo(cc.isGrounded ? PlayerState.Locomotion : PlayerState.Airborne);
    }

    /// <summary>조준 상태. 우클릭으로만 빠져나간다. 점프해도 유지된다.</summary>
    void TickAim(float dt)
    {
        if (input.ConsumeCancel())
        {
            ExitAim();
            return;
        }

        FaceCamera(dt);
        StrafeMove(dt);

        // 조준 중 점프. DoGroundJump()는 Airborne으로 전이해서 조준이
        // 풀리므로, 수직 속도만 직접 준다. 중력 처리는 상태와 무관하게
        // 돌고 있어서 착지까지 자연스럽게 이어진다.
        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            verticalVelocity = Mathf.Sqrt(2f * Mathf.Abs(stats.gravity) * stats.jumpHeight);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }

        // 좌클릭을 누르고 있는 동안에만 시위가 당겨진다
        if (bow) bow.TickDraw(dt, input.AttackHeld);

        if (input.ConsumeAttackReleased())
        {
            if (bow && bow.Fire() && animator) animator.SetTrigger(FireHash);
        }
    }

    void ExitAim()
    {
        TransitionTo(cc.isGrounded ? PlayerState.Locomotion : PlayerState.Airborne);
    }

    // ── 상태 전이 ──────────────────────────────

    void TransitionTo(PlayerState next)
    {
        if (state == next) return;

        if (state == PlayerState.Lunge)
            horizontalVelocity *= stats.lungeExitSpeedRatio;

        if (state == PlayerState.Aim)
        {
            if (bow) bow.CancelAim();
            if (cameraRig) cameraRig.SetAiming(false);
        }

        state = next;

        switch (state)
        {
            case PlayerState.Aim:
                if (bow) bow.BeginAim();
                if (cameraRig) cameraRig.SetAiming(true);
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

        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, rate * dt);

        if (hasInput && control > 0f)
            RotateTowards(dir, dt);
    }

    /// <summary>조준 중 이동. 몸은 카메라를 보고 이동만 옆으로 한다.</summary>
    void StrafeMove(float dt)
    {
        Vector3 dir = CameraRelativeInput();
        bool hasInput = dir.sqrMagnitude > 0.001f;

        Vector3 targetVelocity = hasInput ? dir * stats.aimMoveSpeed : Vector3.zero;
        float rate = hasInput ? stats.acceleration : stats.deceleration;

        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, rate * dt);
    }

    void FaceCamera(float dt)
    {
        if (!cameraTransform) return;

        Vector3 aim = cameraTransform.forward;
        aim.y = 0f;
        if (aim.sqrMagnitude < 0.001f) return;

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.LookRotation(aim.normalized, Vector3.up),
            stats.aimTurnSpeed * dt);

        turnSmoothVelocity = 0f;
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
        if (state == PlayerState.Lunge) return;

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
        lungeCooldownTimer = stats.lungeCooldown;
        TransitionTo(PlayerState.Airborne);
    }

    void StartLunge()
    {
        Vector3 dir = CameraRelativeInput();
        if (dir.sqrMagnitude <= 0.001f) dir = transform.forward;

        dir.y = 0f;
        lungeDirection = dir.normalized;

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
        animator.SetBool(AimHash, state == PlayerState.Aim);
    }
}