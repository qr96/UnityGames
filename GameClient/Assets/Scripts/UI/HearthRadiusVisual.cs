using System.Collections.Generic;
using UnityEngine;

// 화로 온기 반경 표시(눈 녹은 땅). 씬의 모든 화로에 원반을 자동 생성한다.
// 화로의 Melted Ground Visual을 따로 지정했다면 이 컴포넌트는 꺼두면 된다.
public class HearthRadiusVisual : MonoBehaviour
{
    [Header("모양")]
    [SerializeField] private int segments = 48;
    [Tooltip("지면에서 띄우는 높이")]
    [SerializeField] private float heightOffset = 0.03f;

    [Header("디버그")]
    [Tooltip("상태를 콘솔에 출력")]
    [SerializeField] private bool verboseLog = false;

    [Header("색")]
    [SerializeField] private Color litColor = new Color(1f, 0.75f, 0.35f, 0.22f);
    [SerializeField] private Color unlitColor = new Color(0.5f, 0.55f, 0.65f, 0.15f);

    private class Disc
    {
        public GameObject go;
        public MeshRenderer renderer;
        public Material material;
        public float radius = -1f;
        public Mesh mesh;
    }

    private readonly Dictionary<Hearth, Disc> discs = new Dictionary<Hearth, Disc>();
    private float nextReport;

    private void Start()
    {
        if (Shader.Find("Universal Render Pipeline/Unlit") == null &&
            Shader.Find("Sprites/Default") == null)
            Debug.LogWarning("[온기표시] 쓸 수 있는 셰이더를 찾지 못함 — 원반이 보이지 않을 수 있음");
    }

    private void LateUpdate()
    {
        var list = Hearth.All;

        // 1초에 한 번 상태 보고 (원인 추적용)
        if (verboseLog && Time.time >= nextReport)
        {
            nextReport = Time.time + 1f;

            if (list.Count == 0)
            {
                Debug.LogWarning("[온기표시] 씬에 Hearth가 하나도 없음");
            }
            else
            {
                for (int i = 0; i < list.Count; i++)
                {
                    Hearth h = list[i];
                    if (h == null) continue;
                    if (h.WarmthRadius <= 0.01f)
                        Debug.Log($"[온기표시] {h.name}: 반경 0 — 불이 꺼졌거나 연료 없음 " +
                                  $"(불 {h.IsLit}, 연료 {h.Fuel:0.#}/{h.FuelCapacity:0.#})");
                    else
                        Debug.Log($"[온기표시] {h.name}: 반경 {h.WarmthRadius:0.##}, " +
                                  $"원반 위치 {(discs.TryGetValue(h, out Disc dd) && dd.go != null ? dd.go.transform.position.ToString() : "미생성")}");
                }
            }
        }

        // 새 화로 추가
        for (int i = 0; i < list.Count; i++)
        {
            Hearth h = list[i];
            if (h == null) continue;
            if (!discs.TryGetValue(h, out Disc d)) { d = CreateDisc(h); discs[h] = d; }

            // 반경이 바뀌면 메시 재생성
            if (!Mathf.Approximately(d.radius, h.WarmthRadius))
            {
                BuildDisc(d.mesh, h.WarmthRadius);
                d.radius = h.WarmthRadius;
            }

            d.go.transform.position = h.transform.position + Vector3.up * heightOffset;

            Color c = h.IsLit ? litColor : unlitColor;
            if (d.material.HasProperty("_BaseColor")) d.material.SetColor("_BaseColor", c);
            if (d.material.HasProperty("_Color")) d.material.SetColor("_Color", c);
        }

        // 사라진 화로 정리
        if (discs.Count > list.Count)
        {
            var stale = new List<Hearth>();
            foreach (var kv in discs)
                if (kv.Key == null || !list.Contains(kv.Key)) stale.Add(kv.Key);

            for (int i = 0; i < stale.Count; i++)
            {
                if (discs.TryGetValue(stale[i], out Disc d) && d.go != null) Destroy(d.go);
                discs.Remove(stale[i]);
            }
        }
    }

    private Disc CreateDisc(Hearth h)
    {
        var go = new GameObject($"WarmthDisc_{h.name}");
        go.transform.SetParent(transform, false);

        var filter = go.AddComponent<MeshFilter>();
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        var material = new Material(shader);
        SetupTransparent(material);
        renderer.material = material;

        var mesh = new Mesh { name = "WarmthDisc" };
        filter.sharedMesh = mesh;

        return new Disc { go = go, renderer = renderer, material = material, mesh = mesh };
    }

    private static void SetupTransparent(Material m)
    {
        if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
        if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private void BuildDisc(Mesh mesh, float radius)
    {
        int seg = Mathf.Max(8, segments);
        var verts = new Vector3[seg + 2];
        var tris = new int[seg * 3];

        verts[0] = Vector3.zero;
        for (int i = 0; i <= seg; i++)
        {
            float a = (i / (float)seg) * Mathf.PI * 2f;
            verts[i + 1] = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
        }

        // 위에서 내려다볼 때 앞면이 되도록 감는다(순서를 뒤집지 않으면 컬링되어 안 보임)
        for (int i = 0; i < seg; i++)
        {
            tris[i * 3] = 0;
            tris[i * 3 + 1] = i + 2;
            tris[i * 3 + 2] = i + 1;
        }

        var normals = new Vector3[verts.Length];
        for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.up;

        mesh.Clear();
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.normals = normals;
        mesh.RecalculateBounds();
    }
}