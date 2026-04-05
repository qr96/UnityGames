using System.Collections;
using System.Linq;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("전투")]
    public float attackRange = 2f;
    public float hitDelay = 0.3f;
    public float attackCooldown = 1f;
    public int attackDamage = 10;
    public LayerMask enemyLayer;
    public AttackRangeIndicator rangeIndicator;
    public Rigidbody rigid;

    [Header("공격 기준점")]
    [SerializeField] Transform _attackOrigin; // 비워두면 transform.position 사용

    Animator _anim;
    float _cooldownTimer;

    public bool IsAttacking { get; private set; }

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
            var enemy = hit.GetComponent<EnemyHealth>();
            if (enemy == null) continue;

            var hitDir = (hit.transform.position - transform.position).normalized;
            enemy.TakeDamage(attackDamage, hitDir);

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
