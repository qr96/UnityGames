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
public class Projectile : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 20f;

    [Tooltip("이 거리만큼 날아가면 소멸. 발사 주체가 Setup으로 덮어쓸 수 있음.")]
    public float maxDistance = 10f;

    [Header("Behavior")]
    [Tooltip("적을 몇 명까지 관통할지. 1 = 한 명 맞고 사라짐.")]
    public int pierceCount = 1;

    [HideInInspector] public int damage = 1;

    private Rigidbody rb;
    private int remainingPierce;
    private bool initialized = false;
    private Vector3 startPos;
    private float currentSpeed;
    private float effectiveMaxDistance;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        currentSpeed = speed;
        effectiveMaxDistance = maxDistance;
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

        Vector3 scale = transform.localScale;
        if (p.uniformSize > 0f) scale *= p.uniformSize;
        if (p.widthScale > 0f) scale.x *= p.widthScale;
        transform.localScale = scale;

        // Setup이 Start보다 먼저 호출될 수 있으므로 startPos를 여기서도 잡음
        startPos = rb.position;
    }

    void FixedUpdate()
    {
        if (!initialized) return;

        // 거리 기반 소멸
        if (Vector3.Distance(startPos, rb.position) >= effectiveMaxDistance)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 target = rb.position + transform.forward * currentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(target);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Enemy")) return;

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null || enemy.IsDead) return;

        enemy.TakeHit(damage);

        remainingPierce--;
        if (remainingPierce <= 0) Destroy(gameObject);
    }
}