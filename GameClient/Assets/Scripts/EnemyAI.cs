using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public enum State { Idle, Chase, Attack, Die }

    [Header("배회")]
    public float wanderRadius = 5f;
    public float wanderInterval = 3f;

    [Header("감지")]
    public float detectRange = 6f;
    public float attackRange = 1.5f;
    public LayerMask playerLayer;

    [Header("전투")]
    public float attackCooldown = 1.5f;
    public int attackDamage = 5;
    public float hitDelay = 0.3f;

    State _state = State.Idle;
    NavMeshAgent _agent;
    Animator _anim;
    Transform _player;
    float _cooldownTimer;
    float _wanderTimer;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _anim = GetComponent<Animator>();

        // NavMeshAgent 회전은 코드로 직접 제어
        _agent.updateRotation = false;
    }

    void Update()
    {
        if (_state == State.Die) return;

        _cooldownTimer -= Time.deltaTime;

        switch (_state)
        {
            case State.Idle: UpdateIdle(); break;
            case State.Chase: UpdateChase(); break;
            case State.Attack: UpdateAttack(); break;
        }
    }

    void UpdateIdle()
    {
        // 플레이어 감지
        var hits = Physics.OverlapSphere(transform.position, detectRange, playerLayer);
        if (hits.Length > 0)
        {
            _player = hits[0].transform;
            SetState(State.Chase);
            return;
        }

        // 배회
        _wanderTimer -= Time.deltaTime;
        if (_wanderTimer <= 0f)
        {
            _wanderTimer = wanderInterval;

            var randomPoint = GetRandomNavMeshPoint();
            if (randomPoint.HasValue)
            {
                _agent.isStopped = false;
                _agent.SetDestination(randomPoint.Value);
            }
        }

        // 이동 방향으로 회전
        if (_agent.velocity.sqrMagnitude > 0.1f)
        {
            var dir = _agent.velocity.normalized;
            dir.y = 0f;
            transform.rotation = Quaternion.LookRotation(dir);
        }

        // 실제로 움직일 때만 걷기 모션
        if (_anim != null)
            _anim.SetBool("isWalking", _agent.velocity.sqrMagnitude > 0.1f);
    }

    void UpdateChase()
    {
        if (_player == null) { SetState(State.Idle); return; }

        float dist = Vector3.Distance(transform.position, _player.position);

        // 플레이어가 감지 범위 벗어나면 Idle
        if (dist > detectRange * 1.5f)
        {
            _player = null;
            SetState(State.Idle);
            return;
        }

        // 공격 범위 안에 들어오면 Attack
        if (dist <= attackRange)
        {
            SetState(State.Attack);
            return;
        }

        // 추적
        _agent.SetDestination(_player.position);

        // 이동 방향으로 회전
        if (_agent.velocity.sqrMagnitude > 0.1f)
        {
            var dir = _agent.velocity.normalized;
            dir.y = 0f;
            transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    void UpdateAttack()
    {
        if (_player == null) { SetState(State.Idle); return; }

        float dist = Vector3.Distance(transform.position, _player.position);

        // 공격 범위 벗어나면 다시 추적
        if (dist > attackRange)
        {
            SetState(State.Chase);
            return;
        }

        // 플레이어 방향으로 회전
        var lookDir = (_player.position - transform.position).normalized;
        lookDir.y = 0f;
        if (lookDir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(lookDir);

        // 공격
        if (_cooldownTimer <= 0f)
        {
            _cooldownTimer = attackCooldown;
            if (_anim != null) _anim.SetTrigger("attack");
            StartCoroutine(DealDamage());
        }
    }

    Vector3? GetRandomNavMeshPoint()
    {
        // wanderRadius 내 랜덤 방향으로 포인트 시도
        for (int i = 0; i < 5; i++)
        {
            var randomDir = Random.insideUnitSphere * wanderRadius;
            randomDir.y = 0f;
            var candidate = transform.position + randomDir;

            if (NavMesh.SamplePosition(candidate, out var hit, wanderRadius, NavMesh.AllAreas))
                return hit.position;
        }
        return null;
    }

    IEnumerator DealDamage()
    {
        yield return new WaitForSeconds(hitDelay);
        if (_player == null) yield break;

        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist <= attackRange)
        {
            //var health = _player.GetComponent<PlayerHealth>();
            //if (health != null) health.TakeDamage(attackDamage);
        }
    }

    void SetState(State next)
    {
        _state = next;

        _agent.isStopped = next != State.Chase;

        if (_anim != null)
        {
            _anim.SetBool("isWalking", next == State.Chase);
            _anim.SetBool("isAttacking", next == State.Attack);
        }
    }

    public void Die()
    {
        SetState(State.Die);
        _agent.isStopped = true;
        if (_anim != null) _anim.SetTrigger("die");
        Destroy(gameObject, 1.5f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
