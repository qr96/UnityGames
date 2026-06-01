using System;
using UnityEngine;

/// <summary>
/// 적의 HP, 넉백, 죽음. 이동 결정은 EnemyMover에 위임.
/// 같은 GameObject에 EnemyMover를 상속한 컴포넌트가 부착되어 있어야 함.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(EnemyMover))]
public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public int maxHP = 2;

    [Header("Knockback")]
    public float knockbackForce = 5f;
    public float knockbackDecay = 5f;

    [Header("Rewards")]
    public int coinDropAmount = 2;
    public int xpReward = 1;

    [Header("Lifetime")]
    [Tooltip("이 Z 좌표 아래로 가면 자동 제거")]
    public float despawnZ = -15f;

    [Header("Event Channel")]
    public EnemyDiedChannel diedChannel;

    [Tooltip("데미지 발생 시 발행할 채널 (데미지 팝업용). 없으면 발행 안 함.")]
    public DamageDealtChannel damageChannel;

    [Header("Visual")]
    public Animator animator;

    [Tooltip("피격 번쩍임. 비우면 자동 검색.")]
    public HitFlash hitFlash;

    [Tooltip("사망 이펙트 프리팹의 Resources 경로 (예: Effects/EnemyDeath). 비우면 없음.")]
    public string deathEffectPath = "";

    [Tooltip("사망 이펙트 높이 오프셋 (적 발밑 기준 위로). 적 몸통 중앙쯤으로.")]
    public float deathEffectHeight = 1f;

    public event Action<Enemy> OnDied;

    private Rigidbody rb;
    private EnemyMover mover;
    private Vector3 knockbackVelocity = Vector3.zero;
    private int currentHP;
    private bool isDead = false;

    public bool IsDead => isDead;
    public Vector3 Position => rb != null ? rb.position : transform.position;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        mover = GetComponent<EnemyMover>();

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (hitFlash == null) hitFlash = GetComponentInChildren<HitFlash>();
    }

    void Start()
    {
        currentHP = maxHP;
        if (animator != null) animator.SetBool("isWalking", true);
    }

    void FixedUpdate()
    {
        if (isDead) return;

        // 의도된 이동 (AI/Mover가 결정)
        Vector3 velocity = mover.GetVelocity();

        // 넉백은 별도. 총 이동 = velocity + knockback
        Vector3 totalVelocity = velocity + knockbackVelocity;
        Vector3 target = rb.position + totalVelocity * Time.fixedDeltaTime;
        rb.MovePosition(target);

        // 회전: velocity 방향을 봄 (넉백은 무시 — 넉백 중에도 가던 방향 응시)
        if (velocity.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(velocity);
        }

        // 넉백 감쇠
        knockbackVelocity = Vector3.MoveTowards(
            knockbackVelocity, Vector3.zero, knockbackDecay * Time.fixedDeltaTime);

        // 화면 밖 제거
        if (target.z < despawnZ) Destroy(gameObject);
    }

    public void TakeHit(int damage)
    {
        if (isDead) return;

        currentHP -= damage;

        // 체력바용 비율
        float ratio = maxHP > 0 ? Mathf.Clamp01((float)currentHP / maxHP) : 0f;

        // 데미지 시각화 통지 (팝업 + 체력바 둘 다 이 채널로)
        if (damageChannel != null)
        {
            damageChannel.Raise(new DamageInfo
            {
                position = rb.position,
                amount = damage,
                isCritical = false,
                source = this,
                hpRatio = ratio,
            });
        }

        // 피격 번쩍임
        if (hitFlash != null) hitFlash.Flash();

        // 넉백: 현재 이동 방향의 반대로
        Vector3 velocity = mover.GetVelocity();
        if (velocity.sqrMagnitude > 0.0001f)
            knockbackVelocity = -velocity.normalized * knockbackForce;
        else
            knockbackVelocity = Vector3.forward * knockbackForce; // 정지 적이면 +Z로 (플레이어 반대)

        if (currentHP <= 0) Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (diedChannel != null)
        {
            diedChannel.Raise(new EnemyDeathInfo
            {
                position = rb.position,
                coinDropAmount = coinDropAmount,
                xpReward = xpReward,
            });
        }

        // 사망 이펙트 (풀에서 꺼냄)
        if (!string.IsNullOrEmpty(deathEffectPath) && PoolManager.Instance != null)
        {
            if (PoolManager.Instance.TryCreate(deathEffectPath, out GameObject fx))
                fx.transform.position = rb.position + Vector3.up * deathEffectHeight;
        }

        OnDied?.Invoke(this);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (!isDead) OnDied?.Invoke(this);
    }
}