using UnityEngine;

public enum PlayerState
{
    Locomotion,  // 지상 이동
    Airborne,    // 공중 (점프 / 낙하)
    Lunge,       // 공중 도약
    Attack       // 근접 공격
}

/// <summary>
/// 3인칭 백뷰 캐릭터 컨트롤러.
/// 공격은 선딜(Windup) → 판정(Active) → 후딜(Recovery) 3단계로 나뉜다.
/// 판정 구간이 분리되어 있어야 나중에 애니메이션 타이밍을 맞추거나
/// 회피 프레임 같은 걸 붙일 때 구조를 안 건드린다.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputReader))]
public class PlayerController : MonoBehaviour
{
    enum AttackPhase { Windup, Active, Recovery }

    [Header("References")]
    [SerializeField] CharacterStats stats;
    [SerializeField] PlayerInputReader input;
    [Tooltip("비워두면 Camera.main을 자동으로 잡는다")]
    [SerializeField] Transform cameraTransform;
    [Tooltip("비워두면 같은 오브젝트에서 자동으로 찾는다")]
    [SerializeField] MeleeWeapon melee;
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

    // 공격
    AttackPhase attackPhase;
    float attackPhaseTimer;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int GroundedHash = Animator.StringToHash("Grounded");
    static readonly int AttackHash = Animator.StringToHash("Attack");
    static readonly int LungeHash = Animator.StringToHash("Lunge");

    public PlayerState State => state;
    public bool IsGrounded => cc.isGrounded;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (!input) input = GetComponent<PlayerInputReader>();
        if (!melee) melee = GetComponent<MeleeWeapon>();
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;

        if (!stats)
            Debug.LogError($"{name}: CharacterStats가 비어 있다. 인스펙터에 에셋을 꽂아라.", this);
        if (!melee)
            Debug.LogWarning($"{name}: MeleeWeapon이 없다. 공격 판정이 발생하지 않는다.", this);
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
            lungesUsed = 0;
        }
        else
        {
            coyoteTimer -= dt;
        }

        lungeCooldownTimer -= dt;
        attackPhaseTimer -= dt;
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
        horizontalVelocity = lungeDirection * stats.lungeSpeed;
        verticalVelocity = 0f;

        bool blocked = (cc.collisionFlags & CollisionFlags.Sides) != 0;

        if (stateTimer <= 0f || blocked)
            TransitionTo(cc.isGrounded ? PlayerState.Locomotion : PlayerState.Airborne);
    }

    void TickAttack(float dt)
    {
        switch (attackPhase)
        {
            case AttackPhase.Windup:
                StepForward(dt, stats.attackStepSpeed);
                if (attackPhaseTimer <= 0f) EnterAttackPhase(AttackPhase.Active);
                break;

            case AttackPhase.Active:
                StepForward(dt, stats.attackStepSpeed * 0.5f);
                if (melee) melee.HitCheck();
                if (attackPhaseTimer <= 0f) EnterAttackPhase(AttackPhase.Recovery);
                break;

            case AttackPhase.Recovery:
                horizontalVelocity = Vector3.MoveTowards(
                    horizontalVelocity, Vector3.zero, stats.deceleration * dt);

                if (attackPhaseTimer <= 0f)
                    TransitionTo(cc.isGrounded ? PlayerState.Locomotion : PlayerState.Airborne);
                break;
        }
    }

    void EnterAttackPhase(AttackPhase phase)
    {
        attackPhase = phase;

        switch (phase)
        {
            case AttackPhase.Windup:
                attackPhaseTimer = stats.attackWindup;
                break;

            case AttackPhase.Active:
                attackPhaseTimer = stats.attackActive;
                if (melee) melee.BeginSwing();
                break;

            case AttackPhase.Recovery:
                attackPhaseTimer = stats.attackRecovery;
                break;
        }
    }

    /// <summary>공격 중 정면으로 살짝 밀고 나간다. 헛스윙 느낌을 줄여준다.</summary>
    void StepForward(float dt, float speed)
    {
        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity, transform.forward * speed, stats.acceleration * dt);
    }

    // ── 상태 전이 ──────────────────────────────

    void TransitionTo(PlayerState next)
    {
        if (state == next) return;

        if (state == PlayerState.Lunge)
            horizontalVelocity *= stats.lungeExitSpeedRatio;

        state = next;

        switch (state)
        {
            case PlayerState.Attack:
                // 카메라가 보는 수평 방향으로 몸을 돌린 뒤 휘두른다
                if (cameraTransform)
                {
                    Vector3 aim = cameraTransform.forward;
                    aim.y = 0f;
                    if (aim.sqrMagnitude > 0.001f)
                    {
                        transform.rotation = Quaternion.LookRotation(aim.normalized, Vector3.up);
                        turnSmoothVelocity = 0f;
                    }
                }
                EnterAttackPhase(AttackPhase.Windup);
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
    }
}