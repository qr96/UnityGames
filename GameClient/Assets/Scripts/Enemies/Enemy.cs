using System;
using UnityEngine;

/// <summary>
/// 적의 HP, 넉백, 죽음. 이동 결정은 EnemyMover에 위임.
/// 같은 GameObject에 EnemyMover를 상속한 컴포넌트가 부착되어 있어야 함.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(EnemyMover))]
public class Enemy : MonoBehaviour, IDamageable, IPoolable
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

    [Tooltip("사망 이펙트 프리팹 (직접 연결). 비우면 없음. PoolManagerConfig에도 등록 권장.")]
    public GameObject deathEffectPrefab;

    [Tooltip("사망 이펙트 높이 오프셋 (적 발밑 기준 위로). 적 몸통 중앙쯤으로.")]
    public float deathEffectHeight = 1f;

    public event Action<Enemy> OnDied;

    private Rigidbody rb;
    private EnemyMover mover;
    private Vector3 knockbackVelocity = Vector3.zero;
    private int currentHP;
    private bool isDead = false;
    private bool _left = false;   // 퇴장(사망/화면밖) 1회 보장 — OnDied 중복 발행/이중 반납 방지
    private bool _needsPositionSync = false;   // 스폰 후 첫 물리스텝에서 rb.position을 transform과 동기화

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
        // 비풀링(씬 직접 배치)용 1회 초기화. 풀링 시엔 Start가 재호출되지 않으므로
        // 재사용마다 OnSpawn이 ResetState를 담당한다.
        ResetState();
    }

    // ─── 풀링 ───

    /// <summary>풀에서 꺼내질 때마다 호출. 모든 런타임 상태를 초기 상태로 되돌린다.</summary>
    public void OnSpawn()
    {
        ResetState();
    }

    /// <summary>
    /// 스폰 위치/회전을 지정한다. transform과 rb.position을 즉시 함께 설정.
    /// rb.position 직접 대입 = 텔레포트 → 보간 히스토리가 리셋되어
    /// '이전 위치에서 튀어나오는' 잔상이 생기지 않는다.
    /// WaveSpawner가 Get 직후 호출.
    /// </summary>
    public void Spawn(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
        if (rb != null) rb.position = position;
        _needsPositionSync = false;   // 이미 동기화됨
    }

    /// <summary>풀로 반납되기 직전.</summary>
    public void OnDespawn()
    {
        knockbackVelocity = Vector3.zero;
        if (animator != null) animator.SetBool("isWalking", false);
    }

    /// <summary>스폰 시 초기화의 단일 출처. Start/OnSpawn 양쪽에서 호출.</summary>
    private void ResetState()
    {
        currentHP = maxHP;
        isDead = false;
        _left = false;
        knockbackVelocity = Vector3.zero;
        _needsPositionSync = true;   // Spawn()을 안 거치는 경우(비풀링 등) 대비한 fallback
        if (hitFlash != null) hitFlash.ResetFlash();   // 흰색으로 굳은 피격 번쩍임 해제
        if (animator != null) animator.SetBool("isWalking", true);
    }

    void FixedUpdate()
    {
        if (isDead) return;

        // 스폰 직후 1회: rb.position을 실제 스폰 위치(transform)와 동기화.
        // (kinematic+Interpolate는 transform 이동이 rb.position에 즉시 반영되지 않아,
        //  동기화 안 하면 이전 생애의 위치에서부터 이동을 시작함)
        if (_needsPositionSync)
        {
            rb.position = transform.position;
            _needsPositionSync = false;
        }

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

        // 화면 밖 이탈 (보상 없이 퇴장)
        if (target.z < despawnZ) LeavePlay();
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

        // 사망 이펙트 (풀에서 꺼냄). 파티클이면 Poolable이 자동 반납 처리.
        if (deathEffectPrefab != null && PoolManager.Instance != null)
        {
            GameObject fx = PoolManager.Instance.Get(deathEffectPrefab);
            if (fx != null)
            {
                fx.transform.position = rb.position + Vector3.up * deathEffectHeight;
                // 위치 확정 후, 활성화 순간 옛 위치에 방출된 입자 정리
                if (fx.TryGetComponent(out Poolable fxPoolable))
                    fxPoolable.ClearParticles();
            }
        }

        LeavePlay();
    }

    /// <summary>
    /// 적이 플레이에서 빠지는 단일 출구(사망/화면밖 공통).
    /// OnDied를 정확히 한 번 발행하고 풀로 반납한다.
    /// </summary>
    private void LeavePlay()
    {
        if (_left) return;
        _left = true;

        OnDied?.Invoke(this);   // 스포너 생존 카운트 감소용
        ReturnToPool();
    }

    /// <summary>풀이 있으면 반납, 없으면 파괴.</summary>
    private void ReturnToPool()
    {
        if (TryGetComponent(out Poolable poolable)) poolable.ReleaseSelf();
        else Destroy(gameObject);
    }

    void OnDestroy()
    {
        // 풀링 시 정상 퇴장은 LeavePlay에서 처리되므로 여기 도달하지 않음.
        // 씬 종료 등으로 플레이 중 적이 직접 파괴될 때 카운트 누락 방지용 안전망.
        if (!_left) OnDied?.Invoke(this);
    }
}