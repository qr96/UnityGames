using UnityEngine;

/// <summary>
/// 투사체 발사 시 주입할 파라미터 묶음.
/// </summary>
public struct ProjectileSpawnParams
{
    public int damage;
    public int pierce;
    public float maxDistance;   // 이 거리만큼 날아가면 소멸
    public float uniformSize;   // 전체 균등 스케일 배수
    public float widthScale;    // 가로(X) 추가 스케일 배수
    public float speedScale;    // 속도 배수

    public static ProjectileSpawnParams Default(int damage, int pierce, float maxDistance)
    {
        return new ProjectileSpawnParams
        {
            damage = damage,
            pierce = pierce,
            maxDistance = maxDistance,
            uniformSize = 1f,
            widthScale = 1f,
            speedScale = 1f,
        };
    }
}

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour, IPoolable
{
    [Header("Movement")]
    public float speed = 20f;

    [Tooltip("이 거리만큼 날아가면 소멸. 발사 주체가 Setup으로 덮어쓸 수 있음.")]
    public float maxDistance = 10f;

    [Header("Behavior")]
    [Tooltip("적을 몇 명까지 관통할지. 1 = 한 명 맞고 사라짐.")]
    public int pierceCount = 1;

    [Tooltip("이 태그를 가진 대상을 때림. 플레이어 투사체는 'Enemy', 적 투사체는 'Player'.")]
    public string targetTag = "Enemy";

    [HideInInspector] public int damage = 1;

    private Rigidbody rb;
    private int remainingPierce;
    private bool initialized = false;
    private Vector3 startPos;
    private float currentSpeed;
    private float effectiveMaxDistance;
    private Vector3 _baseScale;     // 프리팹 원본 스케일 (재사용 시 누적 방지의 기준)
    private bool _baseScaleCaptured;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        currentSpeed = speed;
        effectiveMaxDistance = maxDistance;

        if (!_baseScaleCaptured)
        {
            _baseScale = transform.localScale;   // 최초 1회만 캡처
            _baseScaleCaptured = true;
        }
    }

    void Start()
    {
        // Setup을 거치지 않고 씬에 직접 배치된 경우만 초기화 (fallback)
        if (!initialized)
        {
            startPos = rb.position;
            remainingPierce = pierceCount;
            initialized = true;
        }
    }

    public void Setup(ProjectileSpawnParams p)
    {
        damage = p.damage;
        pierceCount = p.pierce;
        remainingPierce = p.pierce;
        effectiveMaxDistance = p.maxDistance;
        currentSpeed = speed * p.speedScale;
        initialized = true;

        // 스케일은 항상 원본(_baseScale) 기준으로 재계산 → 재사용해도 누적되지 않음
        Vector3 scale = _baseScale;
        if (p.uniformSize > 0f) scale *= p.uniformSize;
        if (p.widthScale > 0f) scale.x *= p.widthScale;
        transform.localScale = scale;

        // 중요: kinematic + Interpolate rb는 transform을 옮겨도 rb.position이
        // 다음 물리 스텝 전까지 갱신되지 않음. startPos를 transform 기준으로 잡아야
        // 재사용 시 '옛 위치'를 startPos로 잡아 즉시 소멸하는 버그를 막는다.
        startPos = transform.position;
        rb.position = transform.position;   // 물리 위치 즉시 동기화

        // 위치 확정 후 파티클 최종 정리.
        // 활성화 순간(위치 지정 전) Prewarm/PlayOnAwake로 옛 위치에 방출된 입자 제거.
        if (TryGetComponent(out Poolable poolable))
            poolable.ClearParticles();
    }

    void FixedUpdate()
    {
        if (!initialized) return;

        // 거리 기반 소멸
        if (Vector3.Distance(startPos, rb.position) >= effectiveMaxDistance)
        {
            ReturnToPool();
            return;
        }

        Vector3 target = rb.position + transform.forward * currentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(target);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(targetTag)) return;

        IDamageable target = other.GetComponent<IDamageable>();
        if (target == null || target.IsDead) return;

        target.TakeHit(damage);

        remainingPierce--;
        if (remainingPierce <= 0) ReturnToPool();
    }

    // ─── 풀링 ───

    /// <summary>풀에서 꺼내질 때. Setup()이 직후에 호출돼 실제 파라미터를 채운다.</summary>
    public void OnSpawn()
    {
        // 누적 방지: 스케일을 원본으로 되돌림 (Setup이 다시 배수 적용)
        if (_baseScaleCaptured) transform.localScale = _baseScale;
        // Setup이 호출되기 전까지는 움직이지 않도록
        initialized = false;
    }

    /// <summary>풀로 반납되기 직전. 투사체는 코루틴/지속효과가 없어 특별 처리 없음.</summary>
    public void OnDespawn()
    {
        initialized = false;
    }

    /// <summary>풀이 있으면 반납, 없으면 파괴(씬 직접 배치 fallback).</summary>
    private void ReturnToPool()
    {
        if (TryGetComponent(out Poolable poolable)) poolable.ReleaseSelf();
        else Destroy(gameObject);
    }
}