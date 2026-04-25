using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerCombat : MonoBehaviour
{
    [Header("전투")]
    public float attackRange = 2f;
    public float hitDelay = 0.3f;
    public float attackCooldown = 1f;
    public LayerMask enemyLayer;
    public AttackRangeIndicator rangeIndicator;
    public Rigidbody rigid;

    [Header("공격 기준점")]
    [SerializeField] Transform _attackOrigin;

    /// <summary>
    /// 플레이어가 적에게 적중시킬 때마다 발동 (기본공격 + 스킬 투사체 모두).
    /// 확률형 스킬, 흡혈 패시브 등의 트리거로 사용.
    /// </summary>
    public static event Action<IDamageable, int> OnHit;

    /// <summary>
    /// 외부(스킬 투사체 등)에서 적중을 알릴 때 호출.
    /// C# event는 선언 클래스 외부에서 Invoke 불가하므로 헬퍼로 우회.
    /// </summary>
    public static void RaiseOnHit(IDamageable target, int damage)
    {
        OnHit?.Invoke(target, damage);
    }

    Animator _anim;
    float _cooldownTimer;

    public bool IsAttacking { get; private set; }

    // PlayerStats에서 읽기. 없으면 10으로 폴백 (씬 테스트용)
    int AttackDamage => PlayerStats.Instance != null ? PlayerStats.Instance.TotalAttack : 10;

    Vector3 AttackOrigin => _attackOrigin != null ? _attackOrigin.position : transform.position;

    void Awake()
    {
        _anim = GetComponent<Animator>();
        rigid = GetComponent<Rigidbody>();
        rangeIndicator.SetRadius(attackRange);
    }

    void Update()
    {
        _cooldownTimer -= Time.deltaTime;

        if (_cooldownTimer <= 0f)
        {
            IsAttacking = false;
            rangeIndicator.gameObject.SetActive(false);

            var target = GetClosestEnemy();
            if (target != null) Attack(target);
        }
    }

    Transform GetClosestEnemy()
    {
        var hits = Physics.OverlapSphere(AttackOrigin, attackRange, enemyLayer);
        if (hits.Length == 0) return null;

        Transform closest = null;
        float minDist = float.MaxValue;

        foreach (var hit in hits)
        {
            float dist = HorizontalDistance(AttackOrigin, hit.transform.position);
            if (dist < minDist) { minDist = dist; closest = hit.transform; }
        }
        return closest;
    }

    void Attack(Transform target)
    {
        IsAttacking = true;
        _cooldownTimer = attackCooldown;
        rangeIndicator.gameObject.SetActive(true);

        var dir = (target.position - transform.position).normalized;
        dir.y = 0f;
        if (dir != Vector3.zero)
            rigid.rotation = Quaternion.LookRotation(dir);

        if (_anim != null) _anim.SetTrigger("attack");

        StartCoroutine(DamageCo());
    }

    IEnumerator DamageCo()
    {
        yield return new WaitForSeconds(hitDelay);
        DamageTargets(3);
    }

    void DamageTargets(int maxTargetCount)
    {
        var hits = Physics.OverlapSphere(AttackOrigin, attackRange, enemyLayer);
        if (hits.Length == 0) return;

        var targets = hits
            .OrderBy(h => HorizontalDistance(AttackOrigin, h.transform.position))
            .Take(maxTargetCount * 2)
            .OrderBy(_ => Random.value)
            .Take(maxTargetCount);

        foreach (var hit in targets)
        {
            // IDamageable로 추상화 — EnemyStats 타입에 의존하지 않음
            var damageable = hit.GetComponent<IDamageable>();
            if (damageable == null) continue;

            var hitDir = (hit.transform.position - transform.position).normalized;
            damageable.TakeDamage(AttackDamage, hitDir);
            OnHit?.Invoke(damageable, AttackDamage);

            if (PoolManager.Instance.TryCreate("Prefabs/Effects/HCFX_Hit_08", out var effect))
                effect.transform.position = hit.transform.position;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        var origin = _attackOrigin != null ? _attackOrigin.position : transform.position;
        Gizmos.DrawWireSphere(origin, attackRange);
    }

    static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }
}