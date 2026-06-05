using System;
using System.Collections;
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

    [Header("Stats")]
    public int maxHP = 3;

    [Header("Invincibility (피격 무적)")]
    [Tooltip("피격 후 무적 시간(초). 적 무리에 계속 닿아 있으면 이 주기로 다시 맞음.")]
    public float invincibleDuration = 1f;

    [Tooltip("무적 중 깜빡임 간격(초)")]
    public float blinkInterval = 0.1f;

    [Tooltip("무적 점멸의 플래시 강도(0~1). 캐릭터가 사라지지 않고 흰빛으로 맥동.")]
    [Range(0f, 1f)] public float blinkFlashAmount = 0.4f;

    [Tooltip("점멸용 HitFlash. 비우면 자동 검색/부착.")]
    public HitFlash hitFlash;

    public int CurrentHP { get; private set; }
    public int MaxHP => maxHP;

    /// <summary>현재 무적 상태 여부.</summary>
    public bool IsInvincible => Time.time < invincibleUntil;

    /// <summary>HP가 변경됐을 때 발행. UI 갱신용.</summary>
    public event Action OnHPChanged;

    private Rigidbody rb;
    private Vector3 lastMousePos;
    private bool isDragging = false;
    private Vector3 pendingDelta = Vector3.zero;
    private float invincibleUntil = -999f;
    private Coroutine _blinkRoutine;

    void Awake()
    {
        PlayerTransform = transform;
        Instance = this;

        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // 무적 점멸용 HitFlash. 같은 셰이더(_FlashAmount)를 쓰므로 적과 동일 경로로 동작.
        if (hitFlash == null) hitFlash = GetComponentInChildren<HitFlash>();
        if (hitFlash == null) hitFlash = gameObject.AddComponent<HitFlash>();
    }

    void OnDestroy()
    {
        if (PlayerTransform == transform) PlayerTransform = null;
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        CurrentHP = maxHP;
        OnHPChanged?.Invoke();
        if (animator != null) animator.SetBool("isWalking", true);
    }

    void Update()
    {
        ReadKeyboard();
        ReadDrag();
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

    // ─── 패시브 스킬용 메서드 ───

    public void AddMaxHP(int amount)
    {
        maxHP += amount;
        CurrentHP += amount;
        OnHPChanged?.Invoke();
    }

    // ─── 충돌 ───
    // 적 즉사 처리 없음: 적은 살아서 통과하며, 플레이어가 뒤로 쫓아가 잡을 수도 있다.
    // Enter만으론 '계속 닿아 있는 적'에게 무적 해제 후 다시 맞지 않으므로
    // (Enter는 처음 겹칠 때 1회만 발화) Stay를 함께 사용한다.

    void OnTriggerEnter(Collider other) => HandleEnemyContact(other);
    void OnTriggerStay(Collider other) => HandleEnemyContact(other);

    void HandleEnemyContact(Collider other)
    {
        if (!other.CompareTag("Enemy")) return;
        if (IsInvincible) return;   // 무적 중엔 그냥 통과

        TakeDamage(1);
        invincibleUntil = Time.time + invincibleDuration;

        if (_blinkRoutine != null) StopCoroutine(_blinkRoutine);
        _blinkRoutine = StartCoroutine(BlinkWhileInvincible());
    }

    /// <summary>무적 동안 흰빛 점멸. 사라지지 않아 위치가 항상 보임. 끝나면 원상 복구.</summary>
    IEnumerator BlinkWhileInvincible()
    {
        var wait = new WaitForSeconds(blinkInterval);
        bool flashOn = true;

        while (IsInvincible)
        {
            hitFlash.SetFlash(flashOn ? blinkFlashAmount : 0f);
            flashOn = !flashOn;
            yield return wait;
        }

        hitFlash.SetFlash(0f);
        _blinkRoutine = null;
    }

    void TakeDamage(int amount)
    {
        CurrentHP -= amount;
        OnHPChanged?.Invoke();
        if (CurrentHP <= 0)
        {
            if (GameManager.Instance != null) GameManager.Instance.NotifyPlayerDied();
            else Time.timeScale = 0f; // GameManager 없을 때 fallback
        }
    }
}