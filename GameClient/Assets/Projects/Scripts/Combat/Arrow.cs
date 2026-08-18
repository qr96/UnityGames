using UnityEngine;

/// <summary>
/// 포물선을 그리는 화살.
///
/// 매 프레임 이전 위치에서 현재 위치까지 레이캐스트로 검사한다.
/// 빠른 투사체는 단순 콜라이더로는 벽을 그냥 통과해버린다.
///
/// 궤적(TrailRenderer)은 코드에서 자동 생성한다. 머티리얼 설정 없이
/// 어느 파이프라인에서든 보이게 하기 위함이며, 나중에 제대로 된 VFX로 교체할 것.
/// </summary>
public class Arrow : MonoBehaviour
{
    [Header("Flight")]
    [Tooltip("화살에 걸리는 중력. 음수. 클수록 포물선이 급해진다")]
    [SerializeField] float gravity = -18f;

    [Tooltip("이 시간이 지나면 사라진다 (초)")]
    [SerializeField] float lifetime = 8f;

    [Tooltip("박힌 뒤 사라지기까지의 시간 (초)")]
    [SerializeField] float stickDuration = 4f;

    [Header("Collision")]
    [Tooltip("무엇에 맞을지. Damageable + Environment")]
    [SerializeField] LayerMask hitLayers;

    [Tooltip("판정 굵기. 0이면 얇은 레이")]
    [SerializeField] float thickness = 0.05f;

    [Header("Trail")]
    [SerializeField] bool autoCreateTrail = true;
    [SerializeField] float trailTime = 0.35f;
    [SerializeField] float trailWidth = 0.06f;
    [SerializeField] Color trailColor = new Color(1f, 0.95f, 0.7f);

    Vector3 velocity;
    Vector3 previousPosition;
    float damage;
    float knockback;
    GameObject owner;

    bool stuck;
    float timer;

    TrailRenderer trail;

    void Awake()
    {
        if (autoCreateTrail) CreateTrail();
    }

    void CreateTrail()
    {
        trail = GetComponent<TrailRenderer>();
        if (!trail) trail = gameObject.AddComponent<TrailRenderer>();

        trail.time = trailTime;
        trail.startWidth = trailWidth;
        trail.endWidth = 0f;
        trail.minVertexDistance = 0.05f;
        trail.numCapVertices = 2;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;

        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        var mat = new Material(shader);
        mat.SetColor("_BaseColor", trailColor);
        mat.SetColor("_Color", trailColor);
        trail.material = mat;

        trail.startColor = trailColor;
        trail.endColor = trailColor;
    }

    /// <summary>발사 직후 호출. 이 값들은 활이 정한다.</summary>
    public void Launch(Vector3 direction, float speed, float damageAmount,
                       float knockbackAmount, GameObject shooter)
    {
        velocity = direction.normalized * speed;
        damage = damageAmount;
        knockback = knockbackAmount;
        owner = shooter;

        previousPosition = transform.position;
        transform.rotation = Quaternion.LookRotation(velocity, Vector3.up);
        timer = lifetime;

        // 스폰 지점에서 궤적이 튀지 않도록 초기화
        if (trail) trail.Clear();
    }

    void Update()
    {
        float dt = Time.deltaTime;
        timer -= dt;

        if (timer <= 0f) { Destroy(gameObject); return; }
        if (stuck) return;

        velocity += Vector3.up * gravity * dt;
        Vector3 next = transform.position + velocity * dt;

        if (CheckHit(previousPosition, next)) return;

        transform.position = next;
        // 진행 방향으로 눕힌다. 포물선이 눈에 보이는 핵심
        transform.rotation = Quaternion.LookRotation(velocity, Vector3.up);
        previousPosition = next;
    }

    bool CheckHit(Vector3 from, Vector3 to)
    {
        Vector3 delta = to - from;
        float dist = delta.magnitude;
        if (dist < 0.0001f) return false;

        Vector3 dir = delta / dist;

        bool hit = thickness > 0f
            ? Physics.SphereCast(from, thickness, dir, out RaycastHit info,
                                 dist, hitLayers, QueryTriggerInteraction.Collide)
            : Physics.Raycast(from, dir, out info,
                              dist, hitLayers, QueryTriggerInteraction.Collide);

        if (!hit) return false;

        if (owner && info.collider.transform.IsChildOf(owner.transform)) return false;

        var target = info.collider.GetComponentInParent<IDamageable>();
        if (target != null && target.IsAlive)
        {
            var dmg = new DamageInfo(damage, info.point, velocity.normalized, knockback, owner);
            target.TakeDamage(in dmg);
            HitImpactEffect.Play(info.point, -velocity.normalized);

            // 궤적이 남아 있게 렌더러만 끄고 잠시 후 제거
            DetachTrailAndDie();
            return true;
        }

        Stick(info);
        return true;
    }

    void Stick(RaycastHit info)
    {
        stuck = true;
        velocity = Vector3.zero;
        transform.position = info.point;
        timer = stickDuration;

        if (info.collider.transform)
            transform.SetParent(info.collider.transform, true);
    }

    /// <summary>궤적이 뚝 끊기지 않도록 분리한 뒤 화살만 제거한다.</summary>
    void DetachTrailAndDie()
    {
        if (trail)
        {
            trail.transform.SetParent(null, true);
            trail.autodestruct = true;
            Destroy(trail.gameObject, trail.time + 0.1f);
            trail = null;
        }
        Destroy(gameObject);
    }
}