using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class VillagerNPC : MonoBehaviour
{
    public enum WanderType { Random, Waypoint }

    [Header("배회 타입")]
    public WanderType wanderType = WanderType.Random;

    [Header("Random 설정")]
    public float wanderRadius = 5f;
    public float wanderInterval = 3f;

    [Header("Waypoint 설정")]
    public Transform[] waypoints;
    public float waypointWaitTime = 2f;
    public bool loopWaypoints = true;

    [Header("플레이어 감지")]
    public float detectRange = 3f;
    public LayerMask playerLayer;

    NavMeshAgent _agent;
    Animator _anim;
    Transform _player;

    float _wanderTimer;
    float _waitTimer;
    int _waypointIndex;
    bool _isWaiting;
    bool _isLookingAtPlayer;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _anim = GetComponent<Animator>();
        _agent.updateRotation = false;
    }

    void Update()
    {
        DetectPlayer();

        if (_isLookingAtPlayer)
        {
            LookAtPlayer();
            return;
        }

        switch (wanderType)
        {
            case WanderType.Random: UpdateRandom(); break;
            case WanderType.Waypoint: UpdateWaypoint(); break;
        }

        FaceVelocity();
        UpdateAnimation();
    }

    // ── 플레이어 감지 ────────────────────────────────────────────
    void DetectPlayer()
    {
        var hits = Physics.OverlapSphere(transform.position, detectRange, playerLayer);
        if (hits.Length > 0)
        {
            _player = hits[0].transform;
            _isLookingAtPlayer = true;
            _agent.isStopped = true;
        }
        else
        {
            _player = null;
            _isLookingAtPlayer = false;
            _agent.isStopped = false;
        }
    }

    void LookAtPlayer()
    {
        if (_player == null) return;
        var dir = (_player.position - transform.position).normalized;
        dir.y = 0f;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);

        if (_anim != null)
            _anim.SetBool("isWalking", false);
    }

    // ── 랜덤 배회 ────────────────────────────────────────────────
    void UpdateRandom()
    {
        _wanderTimer -= Time.deltaTime;
        if (_wanderTimer <= 0f || (_agent.hasPath && _agent.remainingDistance < 0.3f))
        {
            _wanderTimer = wanderInterval;
            var pt = GetRandomNavMeshPoint();
            if (pt.HasValue)
                _agent.SetDestination(pt.Value);
        }
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

    // ── 웨이포인트 순찰 ──────────────────────────────────────────
    void UpdateWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        if (_isWaiting)
        {
            _waitTimer -= Time.deltaTime;
            if (_waitTimer <= 0f)
            {
                _isWaiting = false;
                MoveToNextWaypoint();
            }
            return;
        }

        if (_agent.hasPath && _agent.remainingDistance < 0.3f)
        {
            _isWaiting = true;
            _waitTimer = waypointWaitTime;

            if (loopWaypoints)
                _waypointIndex = (_waypointIndex + 1) % waypoints.Length;
            else
                _waypointIndex = Mathf.Min(_waypointIndex + 1, waypoints.Length - 1);
        }
    }

    void MoveToNextWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        _agent.SetDestination(waypoints[_waypointIndex].position);
    }

    // ── 공통 ─────────────────────────────────────────────────────
    void FaceVelocity()
    {
        if (_agent.velocity.sqrMagnitude > 0.1f)
        {
            var dir = _agent.velocity.normalized;
            dir.y = 0f;
            transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    void UpdateAnimation()
    {
        if (_anim != null)
            _anim.SetBool("isWalking", _agent.velocity.sqrMagnitude > 0.1f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
}