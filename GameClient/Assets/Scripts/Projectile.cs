using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 20f;
    public float lifeTime = 3f;

    [Header("Combat")]
    public int damage = 1;

    [Tooltip("적을 몇 명까지 관통할지. 1 = 한 명 맞고 사라짐, 2 = 두 명, ...")]
    public int pierceCount = 1;

    private Rigidbody rb;
    private int remainingPierce;
    private bool initialized = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Start()
    {
        // Setup 안 불렸으면 기본값으로 동작
        if (!initialized)
        {
            remainingPierce = pierceCount;
            Destroy(gameObject, lifeTime);
        }
    }

    /// <summary>발사 직후 호출. 스킬 효과 반영된 최종 값으로 초기화.</summary>
    public void Setup(int finalDamage, int finalPierce, float finalLifeTime)
    {
        damage = finalDamage;
        pierceCount = finalPierce;
        remainingPierce = finalPierce;
        lifeTime = finalLifeTime;
        initialized = true;

        // 명시적으로 수명 예약 (Start의 fallback과 중복 방지)
        Destroy(gameObject, finalLifeTime);
    }

    void FixedUpdate()
    {
        Vector3 target = rb.position + transform.forward * speed * Time.fixedDeltaTime;
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