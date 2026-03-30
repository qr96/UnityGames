using System.Collections;
using System.Linq;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class PlayerCombat : MonoBehaviour
{
    [Header("전투")]
    public float attackRange = 2f;
    public float hitDelay = 0.3f;       // 데미지 들어가는 딜레이
    public float attackCooldown = 1f;   // 공격 쿨타임
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
        StartCoroutine(DamageCo());
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    IEnumerator DamageCo()
    {
        yield return new WaitForSeconds(hitDelay);
        DamageTargets(3);
    }

    void DamageTargets(int maxTargetCount)
    {
        var hits = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);
        if (hits.Length == 0)
            return;

        // 가까운 적 중에서 랜덤 타격
        var targets = hits
            .OrderBy(h => Vector3.Distance(transform.position, h.transform.position))
            .Take(maxTargetCount * 2)   // 가까운 적 풀 추리고
            .OrderBy(_ => Random.value)   // 그 안에서 랜덤
            .Take(maxTargetCount);      // 최종 타겟 수만큼

        foreach (var hit in targets)
        {
            var enemy = hit.GetComponent<EnemyHealth>();
            if (enemy == null)
                continue;

            enemy.TakeDamage(attackDamage);

            if (PoolManager.Instance.TryCreate("Effects/HCFX_Hit_08", out var effect))
                effect.transform.position = hit.transform.position + new Vector3(0f, 0.5f, 0f);
        }
    }
}
