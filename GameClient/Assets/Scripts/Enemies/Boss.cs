using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 보스. 잡몹(Enemy)과 별개. IDamageable로 검기/근접에 맞음.
/// 등장 → 지정 위치로 이동(연출) → 전투 → 처치 시 스테이지 클리어.
/// 공격 패턴은 BossAttack(별도, 4단계)에 위임 예정.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Boss : MonoBehaviour, IDamageable
{
    public static Boss ActiveBoss { get; private set; }

    [Header("Stats")]
    public int maxHP = 1000;

    [Header("Entrance (등장 연출)")]
    [Tooltip("등장 후 멈춰서 자리잡을 Z 위치")]
    public float battleZ = 6f;

    [Tooltip("등장 이동 속도")]
    public float entranceSpeed = 4f;

    [Header("Visual")]
    public Animator animator;

    [Header("Damage Channel (선택)")]
    public DamageDealtChannel damageChannel;

    /// <summary>HP 변경 시 (현재, 최대). 보스 HP바가 구독.</summary>
    public event Action<int, int> OnHPChanged;

    /// <summary>보스 등장 시. HP바 표시 등.</summary>
    public event Action<Boss> OnAppeared;

    /// <summary>보스 사망 시.</summary>
    public event Action OnDefeated;

    private Rigidbody rb;
    private int currentHP;
    private bool isDead = false;
    private bool inPosition = false;

    public bool IsDead => isDead;
    public Vector3 Position => rb != null ? rb.position : transform.position;
    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (animator == null) animator = GetComponentInChildren<Animator>();

        ActiveBoss = this;
    }

    void OnDestroy()
    {
        if (ActiveBoss == this) ActiveBoss = null;
    }

    void Start()
    {
        currentHP = maxHP;
        OnAppeared?.Invoke(this);
        OnHPChanged?.Invoke(currentHP, maxHP);
    }

    void FixedUpdate()
    {
        if (isDead) return;

        // 등장 연출: battleZ까지 내려와서 멈춤
        if (!inPosition)
        {
            Vector3 pos = rb.position;
            float newZ = Mathf.MoveTowards(pos.z, battleZ, entranceSpeed * Time.fixedDeltaTime);
            rb.MovePosition(new Vector3(pos.x, pos.y, newZ));

            if (Mathf.Abs(newZ - battleZ) < 0.01f)
            {
                inPosition = true;
                OnEnteredBattle();
            }
        }
    }

    /// <summary>자리를 잡고 전투 시작. 공격 패턴 활성화는 여기서(4단계).</summary>
    void OnEnteredBattle()
    {
        // 4단계에서 BossAttack 컴포넌트 활성화 예정
    }

    public void TakeHit(int damage)
    {
        if (isDead) return;

        currentHP -= damage;

        if (damageChannel != null)
        {
            damageChannel.Raise(new DamageInfo
            {
                position = rb.position,
                amount = damage,
                isCritical = false,
                source = null,   // 보스는 잡몹 체력바 시스템과 무관 (전용 HP바 사용)
                hpRatio = maxHP > 0 ? Mathf.Clamp01((float)currentHP / maxHP) : 0f,
            });
        }

        OnHPChanged?.Invoke(Mathf.Max(0, currentHP), maxHP);

        if (currentHP <= 0) Die();
    }

    /// <summary>보스는 충돌 즉사 면역. 플레이어 몸통박치기로는 데미지를 받지 않는다.</summary>
    public void Kill() { }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        OnDefeated?.Invoke();   // HP바 숨김 등 즉시 반응할 UI용

        // 순서 중요: NotifyBossDefeated를 먼저 부르면 StageClear → timeScale=0이 되어
        // 사망 연출이 시작도 못 하고 얼어붙고, 스케일 시간 기반 지연 파괴도 영영 안 일어남.
        // → 연출을 먼저(아직 BossFight라 timeScale=1), 통지를 나중에.
        StartCoroutine(DeathSequence());
    }

    [Header("Death")]
    [Tooltip("사망 연출 시간(초). 이 시간이 지난 뒤 스테이지 클리어로 전환.")]
    public float deathSequenceDuration = 1f;

    IEnumerator DeathSequence()
    {
        // 사망 애니메이션 트리거 (애니메이터에 추가하면 주석 해제)
        // if (animator != null) animator.SetTrigger("die");

        yield return new WaitForSeconds(deathSequenceDuration);   // BossFight 중이라 timeScale=1, 정상 진행

        if (GameManager.Instance != null)
            GameManager.Instance.NotifyBossDefeated();   // 이제 StageClear(timeScale=0)로 전환해도 안전

        Destroy(gameObject);
    }
}