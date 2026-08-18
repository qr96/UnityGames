using UnityEngine;

/// <summary>
/// 활. 시위 당기기(차지)와 발사를 담당한다.
///
/// 조준 레이는 카메라 위치가 아니라 "캐릭터 평면"에서 출발한다.
/// 카메라는 캐릭터 뒤에 있으므로, 카메라에서 바로 레이를 쏘면
/// 아래 조준 시 캐릭터 뒤쪽 지면이 먼저 잡힌다. 시작점을 카메라→캐릭터
/// 거리만큼 전진시켜 캐릭터 뒤 공간을 검사에서 제외한다.
/// </summary>
public class Bow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] CharacterStats stats;
    [SerializeField] Transform cameraTransform;
    [Tooltip("화살이 나가는 위치. 캐릭터 손 위치에 맞춰 둘 것")]
    [SerializeField] Transform muzzle;
    [SerializeField] Arrow arrowPrefab;

    [Header("Aiming")]
    [Tooltip("조준 레이가 무시할 레이어. Player는 반드시 제외")]
    [SerializeField] LayerMask aimBlockLayers;

    [Tooltip("아무것도 안 맞았을 때 조준할 거리 (m)")]
    [SerializeField] float maxAimDistance = 120f;

    [Tooltip("조준 진입 후 시위를 당기기 시작하기까지의 시간(초)")]
    [SerializeField] float chargeStartDelay = 0.18f;

    [Tooltip("손을 뗐을 때 차지가 풀리는 속도 배수")]
    [SerializeField] float releaseSpeedMultiplier = 4f;

    [SerializeField] float knockback = 2f;

    [Header("Safety")]
    [Tooltip("Stats의 발사 속도가 0이어도 최소한 이 속도로는 나간다")]
    [SerializeField] float fallbackLaunchSpeed = 20f;

    [Header("Trajectory Preview")]
    [SerializeField] bool showTrajectory = true;
    [Tooltip("Arrow 프리팹의 gravity와 같은 값이어야 궤적이 맞는다")]
    [SerializeField] float arrowGravity = -6f;
    [SerializeField] int trajectorySteps = 40;
    [SerializeField] float trajectoryStep = 0.06f;
    [SerializeField] float trajectoryWidth = 0.05f;
    [SerializeField] Color trajectoryColor = new Color(1f, 1f, 1f, 0.6f);

    [Header("Crosshair")]
    [SerializeField] bool drawCrosshair = true;

    [Tooltip("중앙 점 크기 (픽셀)")]
    [SerializeField] float dotSize = 4f;

    [Tooltip("차지 0일 때의 원 반지름 (픽셀)")]
    [SerializeField] float ringRadiusMax = 52f;

    [Tooltip("원을 이루는 점의 크기 (픽셀)")]
    [SerializeField] float ringDotSize = 2.5f;

    [Tooltip("차지 0일 때 원의 불투명도")]
    [Range(0f, 1f)]
    [SerializeField] float ringStartAlpha = 0.85f;

    [SerializeField] Color crosshairColor = Color.white;

    [Header("Debug")]
    [SerializeField] bool logFireRay = false;

    float charge;
    float cooldownTimer;
    float aimTime;
    bool aiming;

    LineRenderer trajectory;
    readonly Vector3[] points = new Vector3[64];

    public float Charge => charge;
    public bool CanFire => cooldownTimer <= 0f;

    public Vector3 MuzzlePosition => muzzle
        ? muzzle.position
        : transform.position + Vector3.up * 1.45f + transform.forward * 0.35f;

    void Awake()
    {
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
        if (aimBlockLayers == 0) aimBlockLayers = ~Layers.PlayerMask;
        if (showTrajectory) CreateTrajectoryLine();

        if (!stats) Debug.LogError($"{name}: CharacterStats가 비어 있다.", this);
    }

    void CreateTrajectoryLine()
    {
        var go = new GameObject("TrajectoryPreview");
        go.transform.SetParent(transform, false);

        trajectory = go.AddComponent<LineRenderer>();
        trajectory.useWorldSpace = true;
        trajectory.startWidth = trajectoryWidth;
        trajectory.endWidth = trajectoryWidth * 0.4f;
        trajectory.numCapVertices = 2;
        trajectory.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trajectory.receiveShadows = false;

        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        var mat = new Material(shader);
        mat.SetColor("_BaseColor", trajectoryColor);
        mat.SetColor("_Color", trajectoryColor);
        trajectory.material = mat;
        trajectory.startColor = trajectoryColor;
        trajectory.endColor = new Color(trajectoryColor.r, trajectoryColor.g, trajectoryColor.b, 0f);
        trajectory.enabled = false;
    }

    // ── 조준 / 차지 ────────────────────────────

    public void BeginAim()
    {
        aiming = true;
        charge = 0f;
        aimTime = 0f;
    }

    public void CancelAim()
    {
        aiming = false;
        charge = 0f;
        aimTime = 0f;
        if (trajectory) trajectory.enabled = false;
    }

    /// <param name="drawing">좌클릭을 누르고 있는가. 눌린 동안에만 시위가 당겨진다.</param>
    public void TickDraw(float dt, bool drawing)
    {
        cooldownTimer -= dt;
        aimTime += dt;

        float rate = stats.drawTime > 0.01f ? dt / stats.drawTime : 1f;

        if (drawing && aimTime >= chargeStartDelay && cooldownTimer <= 0f)
            charge = Mathf.Min(1f, charge + rate);
        else
            charge = Mathf.Max(0f, charge - rate * releaseSpeedMultiplier);

        if (showTrajectory && trajectory) UpdateTrajectory();
    }

    /// <summary>발사. 차지가 0이어도 최소 위력으로 나간다.</summary>
    public bool Fire()
    {
        if (!CanFire || !arrowPrefab) return false;

        Vector3 origin = MuzzlePosition;
        Vector3 dir = FireDirection(origin);

        float speed = LaunchSpeed();
        float damage = Mathf.Lerp(stats.minArrowDamage, stats.maxArrowDamage, charge);

        var arrow = Instantiate(arrowPrefab, origin, Quaternion.LookRotation(dir, Vector3.up));
        arrow.Launch(dir, speed, damage, knockback, gameObject);

        if (logFireRay)
        {
            Debug.DrawRay(origin, dir * 10f, Color.cyan, 3f);
            Debug.Log($"[Bow] charge={charge:0.00} speed={speed:0.0} dir={dir}");
        }

        charge = 0f;
        cooldownTimer = stats.fireCooldown;
        return true;
    }

    float LaunchSpeed()
    {
        float min = stats.minLaunchSpeed;
        float max = stats.maxLaunchSpeed;

        if (min <= 0.01f && max <= 0.01f) return fallbackLaunchSpeed;
        if (max <= 0.01f) max = min;
        if (min <= 0.01f) min = max * 0.4f;

        return Mathf.Lerp(min, max, charge);
    }

    // ── 조준 방향 ──────────────────────────────

    Vector3 FireDirection(Vector3 origin)
    {
        Vector3 dir = GetAimPoint() - origin;

        if (dir.sqrMagnitude < 0.001f)
            dir = cameraTransform ? cameraTransform.forward : transform.forward;

        return dir.normalized;
    }

    /// <summary>
    /// 화면 중앙이 가리키는 월드 지점.
    ///
    /// 레이 시작점은 카메라가 아니라 캐릭터 평면이다:
    /// 카메라 위치에서 시선 방향으로, 카메라→캐릭터 거리만큼 전진한 지점.
    /// 이렇게 하면 캐릭터 뒤쪽 공간(특히 하향 조준 시 뒤쪽 지면)이
    /// 검사에서 제외된다.
    /// </summary>
    Vector3 GetAimPoint()
    {
        if (!cameraTransform)
            return transform.position + transform.forward * maxAimDistance;

        Vector3 camPos = cameraTransform.position;
        Vector3 camDir = cameraTransform.forward;

        // 카메라에서 캐릭터까지의 시선 방향 거리
        float pushDistance = Vector3.Dot(transform.position - camPos, camDir);
        pushDistance = Mathf.Max(0f, pushDistance);

        Vector3 rayStart = camPos + camDir * pushDistance;

        if (Physics.Raycast(rayStart, camDir, out RaycastHit hit,
                            maxAimDistance, aimBlockLayers, QueryTriggerInteraction.Ignore))
        {
            return hit.point;
        }

        return rayStart + camDir * maxAimDistance;
    }

    // ── 궤적 미리보기 ──────────────────────────

    void UpdateTrajectory()
    {
        Vector3 origin = MuzzlePosition;
        Vector3 dir = FireDirection(origin);
        float speed = LaunchSpeed();

        Vector3 pos = origin;
        Vector3 vel = dir * speed;

        int count = Mathf.Min(trajectorySteps, points.Length);
        int used = count;

        for (int i = 0; i < count; i++)
        {
            points[i] = pos;

            Vector3 nextVel = vel + Vector3.up * arrowGravity * trajectoryStep;
            Vector3 nextPos = pos + nextVel * trajectoryStep;

            Vector3 seg = nextPos - pos;
            float segLen = seg.magnitude;

            if (segLen > 0.0001f &&
                Physics.Raycast(pos, seg / segLen, out RaycastHit hit, segLen,
                                aimBlockLayers, QueryTriggerInteraction.Ignore))
            {
                points[i] = hit.point;
                used = i + 1;
                break;
            }

            pos = nextPos;
            vel = nextVel;
        }

        trajectory.enabled = true;
        trajectory.positionCount = used;
        trajectory.SetPositions(points);
    }

    // ── 조준선 ─────────────────────────────────

    void OnGUI()
    {
        if (!drawCrosshair || !aiming) return;

        float cx = Screen.width * 0.5f;
        float cy = Screen.height * 0.5f;

        DrawRect(cx - dotSize * 0.5f, cy - dotSize * 0.5f, dotSize, dotSize, crosshairColor);

        float radius = Mathf.Lerp(ringRadiusMax, dotSize * 0.5f, charge);
        float alpha = ringStartAlpha * (1f - charge);
        if (alpha <= 0.01f) return;

        Color ringC = crosshairColor;
        ringC.a = alpha;

        DrawRing(cx, cy, radius, ringC);
    }

    void DrawRing(float cx, float cy, float radius, Color color)
    {
        float circumference = 2f * Mathf.PI * radius;
        int segments = Mathf.Clamp(Mathf.CeilToInt(circumference / (ringDotSize * 0.5f)), 12, 512);

        for (int i = 0; i < segments; i++)
        {
            float ang = i / (float)segments * Mathf.PI * 2f;
            float x = cx + Mathf.Cos(ang) * radius - ringDotSize * 0.5f;
            float y = cy + Mathf.Sin(ang) * radius - ringDotSize * 0.5f;
            DrawRect(x, y, ringDotSize, ringDotSize, color);
        }
    }

    static void DrawRect(float x, float y, float w, float h, Color c)
    {
        Color prev = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        GUI.color = prev;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(MuzzlePosition, 0.08f);
    }
}