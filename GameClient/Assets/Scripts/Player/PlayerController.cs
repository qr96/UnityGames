using System;
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

    public int CurrentHP { get; private set; }
    public int MaxHP => maxHP;

    /// <summary>HP가 변경됐을 때 발행. UI 갱신용.</summary>
    public event Action OnHPChanged;

    private Rigidbody rb;
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
        CurrentHP -= amount;
        OnHPChanged?.Invoke();
        Debug.Log($"HP: {CurrentHP}/{maxHP}");
        if (CurrentHP <= 0)
        {
            Debug.Log("Game Over");
            Time.timeScale = 0f;
        }
    }
}