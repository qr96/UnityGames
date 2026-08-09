using UnityEngine;

// 조준 원뿔 표시. 차지 중에 대상 방향으로 부채꼴을 그리고, 차지할수록 좁아진다.
// AttackExecutor.OnCharging을 구독한다.
public class AimConeVisual : MonoBehaviour
{
    [SerializeField] private AttackExecutor executor; // 비우면 자기/씬에서 찾음
    [SerializeField] private Transform origin;        // 비우면 이 오브젝트

    [Header("표시")]
    [Tooltip("실제 사거리 대비 원뿔 길이 (1 = 사거리 끝까지)")]
    [Range(0.2f, 1f)]
    [SerializeField] private float rangeScale = 1f;
    [SerializeField] private Color color = new Color(1f, 0.9f, 0.4f, 0.22f);
    [Tooltip("바닥에서 살짝만 띄운다 — 지형에 붙어 보이도록")]
    [SerializeField] private float heightOffset = 0.03f;
    [SerializeField] private int segments = 24;

    private GameObject coneObject;
    private MeshFilter filter;
    private Material material;
    private Mesh mesh;

    private float shownRadius = -1f;
    private float shownAngle = -1f;

    private void Start()
    {
        if (executor == null) executor = GetComponent<AttackExecutor>();
        if (executor == null) executor = FindObjectOfType<AttackExecutor>();
        if (origin == null) origin = transform;

        if (executor != null) executor.OnCharging += OnCharging;

        Create();
    }

    private void OnDestroy()
    {
        if (executor != null) executor.OnCharging -= OnCharging;
    }

    private void Create()
    {
        coneObject = new GameObject("AimCone");

        filter = coneObject.AddComponent<MeshFilter>();
        var renderer = coneObject.AddComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        material = new Material(shader);
        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        renderer.material = material;

        mesh = new Mesh { name = "AimCone" };
        filter.sharedMesh = mesh;

        coneObject.SetActive(false);
    }

    private void OnCharging(AttackPattern pattern, float charge01)
    {
        if (coneObject == null || pattern == null) return;

        // 차지가 끝났거나(0) 원뿔 정보가 없으면 숨긴다
        if (charge01 <= 0f)
        {
            coneObject.SetActive(false);
            return;
        }

        AttackContext ctx = new AttackContext
        {
            attacker = gameObject,
            origin = origin.position,
            forward = executor != null ? executor.AimDirection : origin.forward,
            power = 1,
            charge01 = charge01,
        };

        if (!pattern.TryGetAimCone(ctx, out Vector3 dir, out float angle, out float range))
        {
            coneObject.SetActive(false);
            return;
        }

        float drawRange = range * Mathf.Clamp01(rangeScale);

        if (!Mathf.Approximately(shownRadius, drawRange) || !Mathf.Approximately(shownAngle, angle))
        {
            BuildMesh(drawRange, angle);
            shownRadius = drawRange;
            shownAngle = angle;
        }

        // 지면에 붙이기 — 격자가 있으면 그 칸의 지면 높이를 쓴다
        Vector3 pos = origin.position;
        WorldGrid grid = WorldGrid.Instance;
        if (grid != null) pos.y = grid.SampleHeight(pos);
        coneObject.transform.position = pos + Vector3.up * heightOffset;
        coneObject.transform.rotation = Quaternion.LookRotation(
            dir.sqrMagnitude > 0.0001f ? dir : Vector3.forward, Vector3.up);
        coneObject.SetActive(true);
    }

    // 로컬 +Z 중심 부채꼴
    private void BuildMesh(float radius, float angleDeg)
    {
        int seg = Mathf.Max(3, segments);
        var verts = new Vector3[seg + 2];
        var tris = new int[seg * 3];

        verts[0] = Vector3.zero;
        float half = angleDeg * 0.5f * Mathf.Deg2Rad;

        for (int i = 0; i <= seg; i++)
        {
            float a = -half + (angleDeg * Mathf.Deg2Rad) * (i / (float)seg);
            verts[i + 1] = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * radius;
        }

        for (int i = 0; i < seg; i++)
        {
            tris[i * 3] = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = i + 2;
        }

        mesh.Clear();
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateBounds();
    }
}