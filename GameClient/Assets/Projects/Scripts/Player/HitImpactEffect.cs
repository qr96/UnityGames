using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타격 이펙트. 씬에 하나만 두고 어디서든 HitImpactEffect.Play()로 호출한다.
/// 화살, 근접 공격, 적 공격이 모두 같은 이펙트를 쓴다.
///
/// 파티클 에셋이나 셰이더 없이 원시 도형만 쓰므로 어느 파이프라인에서든 동작한다.
/// 그레이박스용이고, 나중에 제대로 된 VFX로 교체할 것.
///
/// 사용: 빈 오브젝트를 만들어 이 컴포넌트를 붙인다.
/// </summary>
public class HitImpactEffect : MonoBehaviour
{
    static HitImpactEffect instance;

    [Header("Spark")]
    [SerializeField] int sparkCount = 10;
    [SerializeField] float sparkSpeed = 9f;
    [Range(0f, 1f)][SerializeField] float sparkSpeedVariance = 0.5f;
    [SerializeField] float sparkSize = 0.09f;
    [SerializeField] float sparkLifetime = 0.28f;
    [Range(0f, 180f)][SerializeField] float sparkSpread = 75f;
    [SerializeField] float sparkGravity = -18f;

    [Header("Flash")]
    [SerializeField] float flashSize = 0.7f;
    [SerializeField] float flashLifetime = 0.1f;

    [Header("Look")]
    [SerializeField] Color color = new Color(1f, 0.85f, 0.35f);

    Material sharedMat;
    readonly List<Bit> active = new List<Bit>();
    readonly Stack<Transform> pool = new Stack<Transform>();

    struct Bit
    {
        public Transform T;
        public Vector3 Velocity;
        public float Life, MaxLife, StartScale;
        public bool IsFlash;
    }

    /// <summary>타격 지점에 이펙트를 재생한다. 씬에 컴포넌트가 없으면 조용히 무시된다.</summary>
    public static void Play(Vector3 position, Vector3 direction)
    {
        if (instance) instance.Spawn(position, direction);
    }

    void Awake()
    {
        if (instance && instance != this)
        {
            Destroy(this);
            return;
        }
        instance = this;

        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        sharedMat = new Material(shader);
        sharedMat.SetColor("_BaseColor", color);
        sharedMat.SetColor("_Color", color);
    }

    void Spawn(Vector3 pos, Vector3 direction)
    {
        SpawnFlash(pos);

        Vector3 axis = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.up;

        for (int i = 0; i < sparkCount; i++)
        {
            Vector3 dir = RandomCone(axis, sparkSpread);
            float speed = sparkSpeed * Random.Range(1f - sparkSpeedVariance, 1f + sparkSpeedVariance);
            SpawnSpark(pos, dir * speed);
        }
    }

    void SpawnFlash(Vector3 pos)
    {
        var t = Rent(PrimitiveType.Sphere);
        t.position = pos;
        t.localScale = Vector3.one * (flashSize * 0.3f);

        active.Add(new Bit
        {
            T = t,
            Velocity = Vector3.zero,
            Life = flashLifetime,
            MaxLife = flashLifetime,
            StartScale = flashSize,
            IsFlash = true
        });
    }

    void SpawnSpark(Vector3 pos, Vector3 velocity)
    {
        var t = Rent(PrimitiveType.Cube);
        t.position = pos;
        t.localScale = Vector3.one * sparkSize;
        t.rotation = Random.rotation;

        active.Add(new Bit
        {
            T = t,
            Velocity = velocity,
            Life = sparkLifetime,
            MaxLife = sparkLifetime,
            StartScale = sparkSize,
            IsFlash = false
        });
    }

    void Update()
    {
        float dt = Time.deltaTime;

        for (int i = active.Count - 1; i >= 0; i--)
        {
            var b = active[i];
            b.Life -= dt;

            if (b.Life <= 0f)
            {
                Release(b.T);
                active.RemoveAt(i);
                continue;
            }

            float t01 = 1f - b.Life / b.MaxLife;

            if (b.IsFlash)
            {
                b.T.localScale = Vector3.one * (Mathf.Sin(t01 * Mathf.PI) * b.StartScale);
            }
            else
            {
                b.Velocity += Vector3.up * sparkGravity * dt;
                b.T.position += b.Velocity * dt;
                b.T.Rotate(Random.insideUnitSphere * 360f * dt, Space.Self);
                b.T.localScale = Vector3.one * b.StartScale * (1f - t01);
            }

            active[i] = b;
        }
    }

    Transform Rent(PrimitiveType type)
    {
        if (pool.Count > 0)
        {
            var reused = pool.Pop();
            reused.gameObject.SetActive(true);
            return reused;
        }

        var go = GameObject.CreatePrimitive(type);
        go.name = "ImpactBit";
        go.hideFlags = HideFlags.HideInHierarchy;

        var col = go.GetComponent<Collider>();
        if (col) Destroy(col);

        var rend = go.GetComponent<Renderer>();
        rend.sharedMaterial = sharedMat;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;

        return go.transform;
    }

    void Release(Transform t)
    {
        if (!t) return;
        t.gameObject.SetActive(false);
        pool.Push(t);
    }

    static Vector3 RandomCone(Vector3 axis, float halfAngleDeg)
    {
        float angle = Random.Range(0f, halfAngleDeg);
        Vector3 ortho = Vector3.Cross(axis, Random.onUnitSphere).normalized;
        if (ortho.sqrMagnitude < 0.001f) ortho = Vector3.up;

        Vector3 tilted = Quaternion.AngleAxis(angle, ortho) * axis;
        return (Quaternion.AngleAxis(Random.Range(0f, 360f), axis) * tilted).normalized;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
        if (sharedMat) Destroy(sharedMat);
    }
}