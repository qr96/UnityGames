using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 20f;
    public float lifeTime = 3f;

    [Header("Behavior")]
    [Tooltip("적을 몇 명까지 관통할지. 1 = 한 명 맞고 사라짐.")]
    public int pierceCount = 1;

    // 데미지는 발사 주체(ActiveSkill)가 Setup으로 주입.
    // 프리팹에서 직접 설정하지 않도록 Inspector에서 숨김.
    [HideInInspector] public int damage = 1;

    private Rigidbody rb;
    private int remainingPierce;
    private bool initialized = false;
    private float spawnTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        spawnTime = Time.time;
    }

    void Start()
    {
        if (!initialized)
        {
            remainingPierce = pierceCount;
            initialized = true;
        }
    }

    /// <summary>발사 직후 호출. ActiveSkill이 자기 데미지/관통/지속시간/크기를 주입.</summary>
    public void Setup(int damage, int pierce, float lifeTime, float sizeMultiplier)
    {
        this.damage = damage;
        this.pierceCount = pierce;
        this.remainingPierce = pierce;
        this.lifeTime = lifeTime;
        this.initialized = true;

        if (sizeMultiplier > 0f && Mathf.Abs(sizeMultiplier - 1f) > 0.001f)
            transform.localScale *= sizeMultiplier;
    }

    void FixedUpdate()
    {
        if (initialized && Time.time - spawnTime >= lifeTime)
        {
            Destroy(gameObject);
            return;
        }

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