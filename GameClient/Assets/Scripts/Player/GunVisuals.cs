using System.Collections.Generic;
using UnityEngine;

// 총 발사 연출. 탄이 지나간 자리에 연기 자국이 남아 위로 퍼지며 흩어진다.
// AttackExecutor.OnRangedShot을 구독한다.
public class GunVisuals : MonoBehaviour
{
    [SerializeField] private AttackExecutor executor; // 비우면 자기/씬에서 찾음

    [Header("길이")]
    [Tooltip("켜면 명중해도 사거리 끝까지 연기가 남는다. 끄면 맞은 지점까지만")]
    [SerializeField] private bool alwaysFullRange = true;
    [SerializeField] private float muzzleHeight = 1f;

    [Header("연기")]
    [SerializeField] private float fadeSeconds = 1.4f;
    [Tooltip("연기 마디 수 — 많을수록 부드럽게 굽이친다")]
    [SerializeField] private int segments = 14;
    [SerializeField] private float startWidth = 0.1f;
    [Tooltip("시간이 지나며 퍼지는 최대 두께")]
    [SerializeField] private float endWidth = 0.45f;
    [Tooltip("연기가 위로 떠오르는 속도")]
    [SerializeField] private float riseSpeed = 0.55f;
    [Tooltip("옆으로 번지는 정도")]
    [SerializeField] private float driftAmount = 0.35f;
    [Tooltip("발사 직후 자국이 굽이치는 정도")]
    [SerializeField] private float waviness = 0.12f;
    [SerializeField] private Color smokeColor = new Color(0.86f, 0.87f, 0.9f, 0.55f);

    [SerializeField] private int poolSize = 6;

    private class Trail
    {
        public LineRenderer line;
        public Vector3[] basePoints;   // 발사 직후 위치
        public Vector3[] drift;        // 마디별 번지는 방향
        public float timer;
    }

    private readonly List<Trail> pool = new List<Trail>();
    private Material sharedMaterial;
    private AnimationCurve widthCurve;

    private void Start()
    {
        if (executor == null) executor = GetComponent<AttackExecutor>();
        if (executor == null) executor = FindObjectOfType<AttackExecutor>();
        if (executor != null) executor.OnRangedShot += OnShot;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        sharedMaterial = new Material(shader);
        if (sharedMaterial.HasProperty("_Surface")) sharedMaterial.SetFloat("_Surface", 1f);
        if (sharedMaterial.HasProperty("_SrcBlend")) sharedMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (sharedMaterial.HasProperty("_DstBlend")) sharedMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (sharedMaterial.HasProperty("_ZWrite")) sharedMaterial.SetFloat("_ZWrite", 0f);
        sharedMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        sharedMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        // 총구 쪽이 가늘고 끝이 두꺼운 형태
        widthCurve = new AnimationCurve(
            new Keyframe(0f, 0.35f),
            new Keyframe(0.35f, 1f),
            new Keyframe(1f, 0.7f));

        for (int i = 0; i < Mathf.Max(1, poolSize); i++) pool.Add(CreateTrail(i));
    }

    private void OnDestroy()
    {
        if (executor != null) executor.OnRangedShot -= OnShot;
    }

    private Trail CreateTrail(int index)
    {
        var go = new GameObject($"SmokeTrail_{index}");
        go.transform.SetParent(transform, false);

        int n = Mathf.Max(2, segments);

        var line = go.AddComponent<LineRenderer>();
        line.material = sharedMaterial;
        line.positionCount = n;
        line.useWorldSpace = true;
        line.numCapVertices = 4;
        line.numCornerVertices = 2;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.widthCurve = widthCurve;

        go.SetActive(false);
        return new Trail
        {
            line = line,
            basePoints = new Vector3[n],
            drift = new Vector3[n],
            timer = 0f,
        };
    }

    private void OnShot(Vector3 origin, Vector3 dir, float distance, float maxRange, bool hit)
    {
        Trail t = GetFree();
        if (t == null) return;

        float length = alwaysFullRange ? maxRange : distance;
        Vector3 start = origin + Vector3.up * muzzleHeight;
        Vector3 forward = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;
        Vector3 side = Vector3.Cross(Vector3.up, forward);

        int n = t.basePoints.Length;
        for (int i = 0; i < n; i++)
        {
            float k = i / (float)(n - 1);
            Vector3 p = start + forward * (length * k);

            // 발사 직후에도 살짝 굽이치도록
            float wave = Mathf.Sin(k * Mathf.PI * 2.4f + Random.value) * waviness * k;
            p += side * wave;

            t.basePoints[i] = p;

            // 마디마다 번지는 방향(위 + 옆)
            Vector2 r = Random.insideUnitCircle;
            t.drift[i] = (Vector3.up * (0.6f + 0.4f * Random.value)
                          + side * r.x * 0.7f
                          + forward * r.y * 0.3f).normalized;

            t.line.SetPosition(i, p);
        }

        t.timer = Mathf.Max(0.01f, fadeSeconds);
        t.line.gameObject.SetActive(true);
    }

    private Trail GetFree()
    {
        for (int i = 0; i < pool.Count; i++)
            if (pool[i].timer <= 0f) return pool[i];

        Trail oldest = pool[0];
        for (int i = 1; i < pool.Count; i++)
            if (pool[i].timer < oldest.timer) oldest = pool[i];
        return oldest;
    }

    private void Update()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            Trail t = pool[i];
            if (t.timer <= 0f) continue;

            t.timer -= Time.deltaTime;
            float life = 1f - Mathf.Clamp01(t.timer / Mathf.Max(0.01f, fadeSeconds)); // 0 → 1

            // 마디들이 위로 떠오르고 옆으로 번진다
            for (int p = 0; p < t.basePoints.Length; p++)
            {
                Vector3 offset = t.drift[p] * (life * driftAmount)
                                 + Vector3.up * (life * riseSpeed);
                t.line.SetPosition(p, t.basePoints[p] + offset);
            }

            // 퍼지면서 옅어짐
            Color c = smokeColor;
            c.a = smokeColor.a * (1f - life) * (1f - life);
            t.line.startColor = c;
            t.line.endColor = c;

            float w = Mathf.Lerp(startWidth, endWidth, life);
            t.line.widthMultiplier = w;

            if (t.timer <= 0f) t.line.gameObject.SetActive(false);
        }
    }
}