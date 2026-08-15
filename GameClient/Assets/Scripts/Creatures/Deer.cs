using UnityEngine;

// 사냥 조작 검증용 사슴. 비적대·단순 AI.
//  - 배회: 일정 시간마다 방향을 바꿔 천천히 이동
//  - 도망: 플레이어가 감지 거리 안에 들어오면 반대 방향으로 빠르게 이동
//  - 지형: 플레이어와 같은 WorldGrid.CanMoveBetween 규칙을 따른다(경로 탐색은 없음)
// 무리·허기·번식 등 생태 요소는 넣지 않는다. 수치는 전부 테스트 기본값.
public class Deer : MonoBehaviour, IHittable
{
    [Header("체력")]
    [Tooltip("피격 시 무기 위력만큼 감소. 위력 1짜리 총이면 2발")]
    [SerializeField] private int health = 2;

    [Header("감지·이동")]
    [Tooltip("이 거리 안에 플레이어가 들어오면 도망")]
    [SerializeField] private float fleeDistance = 8f;
    [Tooltip("도망을 멈추는 거리(감지 거리보다 크게)")]
    [SerializeField] private float calmDistance = 12f;
    [SerializeField] private float wanderSpeed = 3f;
    [SerializeField] private float fleeSpeed = 7f;
    [SerializeField] private float turnSpeed = 540f;

    [Header("배회")]
    [Tooltip("한 방향으로 움직이는 시간")]
    [SerializeField] private Vector2 wanderMoveSeconds = new Vector2(1.5f, 3f);
    [Tooltip("멈춰 서 있는 시간")]
    [SerializeField] private Vector2 wanderPauseSeconds = new Vector2(1f, 2.5f);

    [Header("드랍")]
    [Tooltip("DroppedItem이 붙은 프리팹")]
    [SerializeField] private GameObject dropPrefab;
    [SerializeField] private ItemDef dropItem;
    [SerializeField] private int dropAmount = 3;
    [SerializeField] private float scatterRadius = 0.6f;

    [Header("참조 (비우면 씬에서 찾음)")]
    [SerializeField] private Transform player;
    [SerializeField] private HitFeedback hitFeedback;

    private enum State { Pause, Wander, Flee }

    private State state = State.Pause;
    private Vector3 moveDir;
    private float stateTimer;
    private int damage;
    private WorldGrid grid;

    // ---- IHittable ----
    public Vector3 HitPosition => transform.position;
    public bool CanBeHit => true;
    public HitCategory Category => HitCategory.Creature;

    public void ApplyHits(int count, GameObject attacker)
    {
        if (count <= 0) return;

        PlayHitFeedback(attacker);

        damage += count;

        // 맞으면 반대 방향으로 즉시 도망
        if (attacker != null) StartFlee(transform.position - attacker.transform.position);

        if (damage >= Mathf.Max(1, health)) Die();
    }

    private void OnEnable() => HittableRegistry.Register(this);
    private void OnDisable() => HittableRegistry.Unregister(this);

    private void Start()
    {
        grid = WorldGrid.Instance != null ? WorldGrid.Instance : FindObjectOfType<WorldGrid>();
        if (hitFeedback == null) hitFeedback = GetComponent<HitFeedback>();

        if (player == null)
        {
            PlayerMovement pm = FindObjectOfType<PlayerMovement>();
            if (pm != null) player = pm.transform;
        }

        EnterPause();
    }

    private void Update()
    {
        UpdateState();
        Move();
        FollowGround();
    }

    // ---- 상태 ----
    private void UpdateState()
    {
        float dist = player != null ? PlanarDistance(transform.position, player.position) : float.MaxValue;

        if (state == State.Flee)
        {
            if (dist > calmDistance) EnterPause();
            else if (player != null) moveDir = AwayFrom(player.position);
            stateTimer -= Time.deltaTime;
            return;
        }

        if (dist <= fleeDistance && player != null)
        {
            StartFlee(transform.position - player.position);
            return;
        }

        stateTimer -= Time.deltaTime;
        if (stateTimer > 0f) return;

        if (state == State.Wander) EnterPause();
        else EnterWander();
    }

    private void StartFlee(Vector3 away)
    {
        state = State.Flee;
        away.y = 0f;
        moveDir = away.sqrMagnitude > 0.0001f ? away.normalized : RandomDirection();
        stateTimer = 1f;
    }

    private void EnterWander()
    {
        state = State.Wander;
        moveDir = RandomDirection();
        stateTimer = Random.Range(wanderMoveSeconds.x, wanderMoveSeconds.y);
    }

    private void EnterPause()
    {
        state = State.Pause;
        moveDir = Vector3.zero;
        stateTimer = Random.Range(wanderPauseSeconds.x, wanderPauseSeconds.y);
    }

    // 지정 지점의 반대 방향(XZ 평면)
    private Vector3 AwayFrom(Vector3 point)
    {
        Vector3 away = transform.position - point;
        away.y = 0f;
        return away.sqrMagnitude > 0.0001f ? away.normalized : moveDir;
    }

    private static Vector3 RandomDirection()
    {
        float a = Random.Range(0f, Mathf.PI * 2f);
        return new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
    }

    // ---- 이동 ----
    private void Move()
    {
        if (state == State.Pause || moveDir.sqrMagnitude < 0.0001f) return;

        float speed = state == State.Flee ? fleeSpeed : wanderSpeed;
        Vector3 delta = moveDir.normalized * speed * Time.deltaTime;

        // 격자 규칙상 막히면 다른 방향을 고른다(경로 탐색 없음)
        if (!CanStep(delta))
        {
            if (!TryPickOpenDirection(speed, out delta))
            {
                EnterPause();
                return;
            }
        }

        transform.position += delta;

        Quaternion target = Quaternion.LookRotation(delta.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
    }

    private bool CanStep(Vector3 delta)
    {
        if (grid == null) return true;
        return grid.CanMoveToWorld(transform.position, transform.position + delta);
    }

    // 현재 방향이 막혔을 때 열린 방향 하나를 찾는다
    private bool TryPickOpenDirection(float speed, out Vector3 delta)
    {
        float step = speed * Time.deltaTime;

        // 도망 중이면 원래 방향에서 조금씩 틀어보고, 배회 중이면 아무 방향이나
        float[] offsets = state == State.Flee
            ? new[] { 35f, -35f, 70f, -70f, 110f, -110f, 180f }
            : new[] { 60f, -60f, 120f, -120f, 180f };

        for (int i = 0; i < offsets.Length; i++)
        {
            Vector3 dir = Quaternion.AngleAxis(offsets[i], Vector3.up) * moveDir.normalized;
            Vector3 d = dir * step;
            if (!CanStep(d)) continue;

            moveDir = dir;
            delta = d;
            return true;
        }

        delta = Vector3.zero;
        return false;
    }

    // 지면 높이 따라가기 (격자가 있으면 경사로 포함)
    private void FollowGround()
    {
        if (grid == null) return;

        Vector3 p = transform.position;
        float groundY = grid.SampleHeight(p);
        p.y = Mathf.Lerp(p.y, groundY, 14f * Time.deltaTime);
        transform.position = p;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f; b.y = 0f;
        return Vector3.Distance(a, b);
    }

    // ---- 사망 ----
    private void Die()
    {
        SpawnDrops();
        Destroy(gameObject);
    }

    private void SpawnDrops()
    {
        if (dropPrefab == null || dropItem == null || dropAmount <= 0)
        {
            Debug.LogWarning($"[사슴] {name}: Drop Prefab 또는 Drop Item 미지정 — 드랍 없음");
            return;
        }

        for (int i = 0; i < dropAmount; i++)
        {
            Vector2 c = Random.insideUnitCircle * scatterRadius;
            Vector3 pos = transform.position + new Vector3(c.x, 0f, c.y);

            GameObject go = Instantiate(dropPrefab, pos,
                Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));

            DroppedItem drop = go.GetComponent<DroppedItem>();
            if (drop != null) drop.Setup(dropItem, 1);
        }
    }

    private void PlayHitFeedback(GameObject attacker)
    {
        if (hitFeedback == null) hitFeedback = GetComponent<HitFeedback>();
        if (hitFeedback == null) return;

        Vector3 dir = attacker != null
            ? transform.position - attacker.transform.position
            : Vector3.zero;
        hitFeedback.Play(dir);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.7f);
        DrawCircle(fleeDistance);
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.4f);
        DrawCircle(calmDistance);
    }

    private void DrawCircle(float radius)
    {
        const int seg = 32;
        Vector3 prev = transform.position + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= seg; i++)
        {
            float a = (i / (float)seg) * Mathf.PI * 2f;
            Vector3 p = transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
    }
}