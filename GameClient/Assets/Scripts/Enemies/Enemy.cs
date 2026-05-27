using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    public int maxHP = 2;
    public float speed = 8f;

    [Header("Knockback")]
    public float knockbackForce = 5f;
    public float knockbackDecay = 5f;

    [Header("Rewards")]
    public int coinDropAmount = 2;
    public int xpReward = 1;

    [Header("Event Channel")]
    public EnemyDiedChannel diedChannel;

    [Header("Visual")]
    [Tooltip("비워두면 자식에서 자동으로 찾음")]
    public Animator animator;

    public event Action<Enemy> OnDied;

    // 이동 방향. 나중에 좌우/곡선 이동 추가 시 이 벡터만 바꾸면 됨.
    private Vector3 moveDirection = Vector3.back;

    private Rigidbody rb;
    private int currentHP;
    private float knockbackVelocity = 0f;
    private bool isDead = false;

    public bool IsDead => isDead;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (animator == null) animator = GetComponentInChildren<Animator>();

        // 이동 방향과 forward 일치
        FaceMoveDirection();
    }

    void Start()
    {
        currentHP = maxHP;
        if (animator != null) animator.SetBool("isWalking", true);
    }

    void FixedUpdate()
    {
        if (isDead) return;

        // forward 방향으로 이동 + 넉백은 forward 반대 방향
        Vector3 delta = transform.forward * speed * Time.fixedDeltaTime
                      - transform.forward * knockbackVelocity * Time.fixedDeltaTime;
        Vector3 target = rb.position + delta;
        rb.MovePosition(target);

        knockbackVelocity = Mathf.MoveTowards(knockbackVelocity, 0f, knockbackDecay * Time.fixedDeltaTime);

        if (target.z < -15f) Destroy(gameObject);
    }

    /// <summary>이동 방향 변경. 호출 시 자동으로 그 방향을 바라봄.</summary>
    public void SetMoveDirection(Vector3 direction)
    {
        moveDirection = direction.normalized;
        FaceMoveDirection();
    }

    void FaceMoveDirection()
    {
        if (moveDirection.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(moveDirection);
    }

    public void TakeHit(int damage)
    {
        if (isDead) return;

        currentHP -= damage;
        // 넉백은 forward 반대 방향 = 뒤로 밀려남
        knockbackVelocity = knockbackForce;

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