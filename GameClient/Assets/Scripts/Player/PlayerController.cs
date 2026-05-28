using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    public static Transform PlayerTransform { get; private set; }
    public static PlayerController Instance { get; private set; }

    public Animator animator;

    [Header("Movement")]
    public float keyboardSpeed = 8f;
    public float dragSensitivity = 0.02f;
    public float minX = -4f, maxX = 4f;
    public float minZ = -2f, maxZ = 2f;

    [Header("Primary Fire (탄환)")]
    public GameObject projectilePrefab;
    public float fireInterval = 0.5f;

    [Tooltip("동시에 발사되는 탄환 수 (1=중앙만, 3=좌중우, ...)")]
    public int projectileCount = 1;

    [Tooltip("여러 발일 때 좌우로 벌어지는 각도(도)")]
    public float spreadAngle = 15f;

    [Tooltip("모든 탄환 데미지에 곱해지는 배수")]
    public float damageMultiplier = 1f;

    [Tooltip("관통 횟수 보너스 (스킬로 누적)")]
    public int bonusPierce = 0;

    [Header("Secondary Fire (검기)")]
    [Tooltip("검기 프리팹 (Projectile 컴포넌트 부착)")]
    public GameObject swordWavePrefab;

    [Tooltip("검기 발사 개수 (스킬로 증가). 0이면 발사 안 함")]
    public int swordWaveCount = 0;

    [Tooltip("검기 발사 간격(초)")]
    public float swordWaveInterval = 1.2f;

    [Tooltip("검기 데미지")]
    public int swordWaveDamage = 2;

    [Tooltip("검기 지속시간에 더해지는 보너스(초) — 스킬로 누적")]
    public float swordWaveLifeBonus = 0f;

    [Header("Stats")]
    public int maxHP = 3;

    private Rigidbody rb;
    private int currentHP;
    private float fireTimer = 0f;
    private float swordWaveTimer = 0f;
    private Vector3 lastMousePos;
    private bool isDragging = false;

    private Vector3 pendingDelta = Vector3.zero;

    void Awake()
    {
        PlayerTransform = transform;
        Instance = this;

        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void OnDestroy()
    {
        if (PlayerTransform == transform) PlayerTransform = null;
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        currentHP = maxHP;
        if (animator != null) animator.SetBool("isWalking", true);
    }

    void Update()
    {
        ReadKeyboard();
        ReadDrag();

        fireTimer += Time.deltaTime;
        if (fireTimer >= fireInterval)
        {
            FirePrimary();
            fireTimer = 0f;
        }

        if (swordWaveCount > 0)
        {
            swordWaveTimer += Time.deltaTime;
            if (swordWaveTimer >= swordWaveInterval)
            {
                FireSwordWave();
                swordWaveTimer = 0f;
            }
        }
    }

    void FixedUpdate()
    {
        if (pendingDelta.sqrMagnitude > 0f)
        {
            Vector3 target = rb.position + pendingDelta;
            target.x = Mathf.Clamp(target.x, minX, maxX);
            target.z = Mathf.Clamp(target.z, minZ, maxZ);
            rb.MovePosition(target);
            pendingDelta = Vector3.zero;
        }
    }

    void ReadKeyboard()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        if (h == 0f && v == 0f) return;

        Vector3 dir = new Vector3(h, 0f, v).normalized;
        pendingDelta += dir * keyboardSpeed * Time.deltaTime;
    }

    void ReadDrag()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (Input.mousePosition.y < Screen.height * 0.5f)
            {
                isDragging = true;
                lastMousePos = Input.mousePosition;
            }
        }
        if (Input.GetMouseButtonUp(0)) isDragging = false;

        if (isDragging)
        {
            Vector3 delta = Input.mousePosition - lastMousePos;
            pendingDelta += new Vector3(delta.x * dragSensitivity, 0f, delta.y * dragSensitivity);
            lastMousePos = Input.mousePosition;
        }
    }

    void FirePrimary()
    {
        if (projectilePrefab == null) return;

        Vector3 spawnPos = rb.position + Vector3.forward * 1f;
        int count = Mathf.Max(1, projectileCount);

        float startAngle = -(count - 1) * 0.5f * spreadAngle;

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + i * spreadAngle;
            Quaternion rot = Quaternion.Euler(0f, angle, 0f);
            GameObject p = Instantiate(projectilePrefab, spawnPos, rot);

            Projectile proj = p.GetComponent<Projectile>();
            if (proj != null)
            {
                int finalDamage = Mathf.RoundToInt(proj.damage * damageMultiplier);
                int finalPierce = proj.pierceCount + bonusPierce;
                proj.Setup(finalDamage, finalPierce, proj.lifeTime);
            }
        }

        if (animator != null) animator.SetTrigger("attack");
    }

    void FireSwordWave()
    {
        if (swordWavePrefab == null) return;

        for (int i = 0; i < swordWaveCount; i++)
        {
            Vector3 spawnPos = rb.position + Vector3.forward * (1f + i * 0.5f);
            GameObject w = Instantiate(swordWavePrefab, spawnPos, Quaternion.identity);

            Projectile proj = w.GetComponent<Projectile>();
            if (proj != null)
            {
                int finalDamage = Mathf.RoundToInt(swordWaveDamage * damageMultiplier);
                int finalPierce = proj.pierceCount + bonusPierce;
                // 검기 전용 지속시간 보너스 적용
                float finalLifeTime = proj.lifeTime + swordWaveLifeBonus;
                proj.Setup(finalDamage, finalPierce, finalLifeTime);
            }
        }
    }

    // ─── 스킬 메서드 ───

    public void ModifyFireInterval(float multiplier)
    {
        fireInterval = Mathf.Max(0.05f, fireInterval * multiplier);
    }

    public void AddProjectileCount(int amount)
    {
        projectileCount += amount;
    }

    public void AddBonusPierce(int amount)
    {
        bonusPierce += amount;
    }

    public void MultiplyDamage(float multiplier)
    {
        damageMultiplier *= multiplier;
    }

    public void AddMaxHP(int amount)
    {
        maxHP += amount;
        currentHP += amount;
    }

    public void AddSwordWave(int amount)
    {
        swordWaveCount += amount;
    }

    public void AddSwordWaveLifeTime(float seconds)
    {
        swordWaveLifeBonus += seconds;
    }

    // ─── 충돌 ───

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            TakeDamage(1);

            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null) enemy.TakeHit(999);
            else Destroy(other.gameObject);
        }
    }

    void TakeDamage(int amount)
    {
        currentHP -= amount;
        Debug.Log($"HP: {currentHP}/{maxHP}");
        if (currentHP <= 0)
        {
            Debug.Log("Game Over");
            Time.timeScale = 0f;
        }
    }
}