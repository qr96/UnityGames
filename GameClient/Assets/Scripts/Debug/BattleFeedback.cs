using UnityEngine;

/// <summary>
/// 개발용 히트 피드백. 빈 오브젝트에 붙이기만 하면 동작 (레퍼런스 연결 불필요).
/// - 적 타격: 데미지 플로팅 텍스트
/// - 벽 반사: 축소 소멸하는 구체 마커
/// BattleEvents 구독만으로 구현 → 기존 전투 코드 무수정.
/// 프로토타입 등급: Destroy 기반이므로 이후 풀링 + TMP 버전으로 교체 대상.
///
/// [주의] 구독은 반드시 Start에서. BattleManager.Awake가 ClearAll()을 호출하므로
/// OnEnable/Awake 구독은 실행 순서에 따라 지워질 수 있음. (이벤트 버스 구독 규칙)
///
/// [수정] 런타임 생성 TextMesh는 폰트가 null이라 렌더링되지 않음
///        → 내장 폰트 + 폰트 머티리얼을 명시적으로 할당.
/// </summary>
public class BattleFeedback : MonoBehaviour
{
    [Header("데미지 텍스트")]
    [SerializeField] float textLife = 0.6f;
    [SerializeField] float floatSpeed = 2.5f;
    [SerializeField] int fontSize = 48;
    [SerializeField] float charSize = 0.08f;
    [SerializeField] Color damageColor = new Color(1f, 0.9f, 0.2f);

    [Header("벽 반사 마커")]
    [SerializeField] float pingLife = 0.25f;
    [SerializeField] float pingScale = 0.35f;

    [Header("디버그")]
    [Tooltip("이벤트 수신 시 콘솔 로그 (피드백이 안 보일 때 이벤트 자체가 오는지 확인용)")]
    [SerializeField] bool logEvents = false;

    Camera _cam;
    Font _font;

    void Start()
    {
        _cam = Camera.main;
        _font = LoadBuiltinFont();
        if (_font == null)
            Debug.LogError("[BattleFeedback] 내장 폰트 로드 실패 — 데미지 텍스트가 표시되지 않음", this);

        BattleEvents.EnemyHit += OnEnemyHit;
        BattleEvents.WallBounced += OnWallBounced;
    }

    void OnDestroy()
    {
        BattleEvents.EnemyHit -= OnEnemyHit;
        BattleEvents.WallBounced -= OnWallBounced;
    }

    static Font LoadBuiltinFont()
    {
        // Unity 2022.2+ 내장 폰트 이름 → 실패 시 구버전 이름 폴백
        try { return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
        catch { /* fall through */ }
        try { return Resources.GetBuiltinResource<Font>("Arial.ttf"); }
        catch { return null; }
    }

    void OnEnemyHit(Projectile p, Enemy e, int dmg)
    {
        if (logEvents) Debug.Log($"[Feedback] EnemyHit: {e.name} -{dmg}");
        SpawnText(e.transform.position + Vector3.up * 1.3f, dmg.ToString(), damageColor);
    }

    void OnWallBounced(Projectile p, Vector3 pos)
    {
        if (logEvents) Debug.Log($"[Feedback] WallBounced @ {pos}");
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(go.GetComponent<Collider>()); // 반사 판정 오염 방지
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * pingScale;
        go.AddComponent<AutoShrink>().life = pingLife;
    }

    void SpawnText(Vector3 pos, string text, Color color)
    {
        if (_font == null) return;

        var go = new GameObject("DmgText");
        go.transform.position = pos;
        go.transform.rotation = _cam.transform.rotation; // 카메라와 같은 방향 = 항상 정면

        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.font = _font;                                   // 핵심 수정 1: 폰트 할당
        tm.fontSize = fontSize;
        tm.characterSize = charSize;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = color;
        go.GetComponent<MeshRenderer>().material = _font.material; // 핵심 수정 2: 폰트 머티리얼

        var anim = go.AddComponent<FloatAndFade>();
        anim.life = textLife;
        anim.speed = floatSpeed;
    }

    // ---- 1회성 애니메이션 헬퍼 (외부 노출 불필요라 중첩 클래스) ----

    class FloatAndFade : MonoBehaviour
    {
        public float life = 0.6f;
        public float speed = 2.5f;
        float _t;
        TextMesh _tm;

        void Awake() => _tm = GetComponent<TextMesh>();

        void Update()
        {
            _t += Time.deltaTime;
            transform.position += Vector3.up * (speed * Time.deltaTime);
            if (_tm != null)
            {
                var c = _tm.color;
                c.a = Mathf.Clamp01(1f - _t / life);
                _tm.color = c;
            }
            if (_t >= life) Destroy(gameObject);
        }
    }

    class AutoShrink : MonoBehaviour
    {
        public float life = 0.25f;
        float _t;
        Vector3 _s0;

        void Start() => _s0 = transform.localScale;

        void Update()
        {
            _t += Time.deltaTime;
            transform.localScale = _s0 * Mathf.Max(0f, 1f - _t / life);
            if (_t >= life) Destroy(gameObject);
        }
    }
}