using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("전투")]
    public float attackRange = 2f;
    public float attackCooldown = 1f;
    public int attackDamage = 10;
    public LayerMask enemyLayer;
    public AttackRangeIndicator rangeIndicator;
    public Rigidbody rigid;

    Animator _anim;
    float _cooldownTimer;

    public bool IsAttacking { get; private set; }

    void Awake()
    {
        _anim = GetComponent<Animator>();
        rigid = GetComponent<Rigidbody>();
        rangeIndicator.SetRadius(attackRange);
    }

    private void Update()
    {
        _cooldownTimer -= Time.deltaTime;

        if (_cooldownTimer <= 0f)
        {
            IsAttacking = false;
            rangeIndicator.gameObject.SetActive(false);

            var target = GetClosestEnemy();
            if (target != null)
                Attack(target);
        }
    }

    Transform GetClosestEnemy()
    {
        var hits = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);
        if (hits.Length == 0) return null;

        Transform closest = null;
        float minDist = float.MaxValue;

        foreach (var hit in hits)
        {
            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = hit.transform;
            }
        }

        return closest;
    }

    void Attack(Transform target)
    {
        IsAttacking = true;
        _cooldownTimer = attackCooldown;
        rangeIndicator.gameObject.SetActive(true);

        // 공격 방향으로 회전
        var dir = (target.position - transform.position).normalized;
        dir.y = 0f;
        if (dir != Vector3.zero)
            rigid.rotation = Quaternion.LookRotation(dir);

        // 애니메이션
        if (_anim != null)
            _anim.SetTrigger("attack");

        // 데미지
        var enemy = target.GetComponent<EnemyHealth>();
        if (enemy != null)
            enemy.TakeDamage(attackDamage);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
