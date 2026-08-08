using UnityEngine;

// 스윙 궤적. 판정 순간에 히트박스와 같은 수치(반경·각도)의 반투명 부채꼴을 띄우고 페이드아웃.
// 아트 없이 런타임 메시로 만든다.
public class SwingArcVisual : MonoBehaviour
{
    [SerializeField] private AttackExecutor executor; // 비우면 자기/씬에서 찾음
    [Tooltip("궤적을 띄울 기준(보통 플레이어). 비우면 이 오브젝트")]
    [SerializeField] private Transform origin;

    [Header("표시")]
    [SerializeField] private float showSeconds = 0.15f;
    [SerializeField] private Color color = new Color(1f, 0.95f, 0.6f, 0.35f);
    [Tooltip("바닥에서 살짝 띄우기")]
    [SerializeField] private float heightOffset = 0.15f;
    [SerializeField] private int segments = 24;

    private GameObject arcObject;
    private MeshFilter filter;
    private MeshRenderer meshRenderer;
    private Material material;
    private Mesh mesh;

    private float timer;

    private void Start()
    {
        if (executor == null) executor = GetComponent<AttackExecutor>();
        if (executor == null) executor = FindObjectOfType<AttackExecutor>();
        if (origin == null) origin = transform;

        if (executor != null) executor.OnSwingHit += Show;

        CreateArcObject();
    }

    private void OnDestroy()
    {
        if (executor != null) executor.OnSwingHit -= Show;
    }

    private void CreateArcObject()
    {
        arcObject = new GameObject("SwingArc");
        arcObject.transform.SetParent(null);

        filter = arcObject.AddComponent<MeshFilter>();
        meshRenderer = arcObject.AddComponent<MeshRenderer>();
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        material = new Material(shader);
        SetupTransparent(material);
        meshRenderer.material = material;

        mesh = new Mesh { name = "SwingArc" };
        filter.sharedMesh = mesh;

        arcObject.SetActive(false);
    }

    private static void SetupTransparent(Material m)
    {
        // URP Unlit을 반투명으로
        if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
        if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0f);
        if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private void Show(AttackPattern pattern, Vector3 aim)
    {
        if (pattern == null || arcObject == null) return;
        if (!pattern.TryGetArc(out float radius, out float angleDeg)) return;

        BuildMesh(radius, angleDeg);

        Vector3 pos = origin.position + Vector3.up * heightOffset;
        arcObject.transform.position = pos;
        arcObject.transform.rotation = Quaternion.LookRotation(
            aim.sqrMagnitude > 0.0001f ? aim : Vector3.forward, Vector3.up);

        arcObject.SetActive(true);
        timer = Mathf.Max(0.01f, showSeconds);
        SetAlpha(color.a);
    }

    // 로컬 +Z를 중심으로 angleDeg만큼 벌어진 부채꼴
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

    private void SetAlpha(float a)
    {
        Color c = color; c.a = a;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", c);
        if (material.HasProperty("_Color"))     material.SetColor("_Color", c);
    }

    private void Update()
    {
        if (timer <= 0f) return;

        timer -= Time.deltaTime;
        float k = Mathf.Clamp01(timer / Mathf.Max(0.01f, showSeconds));
        SetAlpha(color.a * k);   // 페이드아웃

        if (timer <= 0f) arcObject.SetActive(false);
    }
}
