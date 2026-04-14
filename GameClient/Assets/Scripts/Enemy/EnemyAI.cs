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
    public float hitDelay = 0.3f;

    State _state = State.Idle;
    NavMeshAgent _agent;
    Animator _anim;
    EnemyKnockback _knockback;
    EnemyStats _stats;
    Transform _player;
    float _cooldownTimer;
    float _wanderTimer;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _anim = GetComponent<Animator>();
        _knockback = GetComponent<EnemyKnockback>();
        _stats = GetComponent<EnemyStats>();

        _agent.updateRotation = false;
        _stats.OnDied += OnDied;
    }

    void Update()
    {
        if (_state == State.Die) return;
        if (_knockback != null && _knockback.IsKnockedBack) return;

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
        var hits = Physics.OverlapSphere(transform.position, detectRange, playerLayer);
        if (hits.Length > 0)
        {
            _player = hits[0].transform;
            SetState(State.Chase);
            return;
        }

        _wanderTimer -= Time.deltaTime;
        if (_wanderTimer <= 0f)
        {
            _wanderTimer = wanderInterval;
            var pt = GetRandomNavMeshPoint();
            if (pt.HasValue)
            {
                _agent.isStopped = false;
                _agent.SetDestination(pt.Value);
            }
        }

        FaceVelocity();
        if (_anim != null)
            _anim.SetBool("isWalking", _agent.velocity.sqrMagnitude > 0.1f);
    }

    void UpdateChase()
    {
        if (_player == null) { SetState(State.Idle); return; }

        float dist = HorizontalDistance(transform.position, _player.position);
        if (dist > detectRange * 1.5f) { _player = null; SetState(State.Idle); return; }
        if (dist <= attackRange) { SetState(State.Attack); return; }

        _agent.SetDestination(_player.position);
        FaceVelocity();
    }

    void UpdateAttack()
    {
        if (_player == null) { SetState(State.Idle); return; }

        float dist = HorizontalDistance(transform.position, _player.position);
        if (dist > attackRange) { SetState(State.Chase); return; }

        FaceTarget(_player.position);

        if (_cooldownTimer <= 0f)
        {
            _cooldownTimer = attackCooldown;
            if (_anim != null) _anim.SetTrigger("attack");
            StartCoroutine(DealDamage());
        }
    }

    IEnumerator DealDamage()
    {
        yield return new WaitForSeconds(hitDelay);
        if (_player == null) yield break;
        if (HorizontalDistance(transform.position, _player.position) > attackRange) yield break;

        // IDamageable로 추상화 — PlayerStats 타입에 직접 의존하지 않음
        var damageable = _player.GetComponent<IDamageable>();
        damageable?.TakeDamage(_stats.Data.attackDamage);
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

    void OnDied()
    {
        SetState(State.Die);
        _agent.isStopped = true;
        if (_anim != null) _anim.SetTrigger("die");
        gameObject.SetActive(false);
    }

    void FaceVelocity()
    {
        if (_agent.velocity.sqrMagnitude > 0.1f)
        {
            var dir = _agent.velocity.normalized;
            dir.y = 0f;
            transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    void FaceTarget(Vector3 pos)
    {
        var dir = (pos - transform.position).normalized;
        dir.y = 0f;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    Vector3? GetRandomNavMeshPoint()
    {
        for (int i = 0; i < 5; i++)
        {
            var candidate = transform.position + new Vector3(
                Random.Range(-wanderRadius, wanderRadius), 0f,
                Random.Range(-wanderRadius, wanderRadius));

            if (NavMesh.SamplePosition(candidate, out var hit, wanderRadius, NavMesh.AllAreas))
                return hit.position;
        }
        return null;
    }

    static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
