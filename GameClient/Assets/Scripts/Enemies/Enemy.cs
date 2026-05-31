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

    [Header("Visual")]
    public Animator animator;

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

        OnDied?.Invoke(this);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (!isDead) OnDied?.Invoke(this);
    }
}