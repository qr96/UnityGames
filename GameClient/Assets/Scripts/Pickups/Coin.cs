using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class Coin : MonoBehaviour
{
    /// <summary>스킬로 누적되는 전역 자석 보너스. 모든 코인에 즉시 적용됨.</summary>
    public static float GlobalMagnetBonus = 0f;

    [Header("Movement")]
    public float moveSpeedZ = 8f;
    public float rotateSpeed = 180f;

    [Header("Spawn kick")]
    [Tooltip("등장 시 뒤(+Z)로 튕기는 초기 속도. moveSpeedZ보다 커야 실제로 뒤로 간다.")]
    public float launchSpeed = 12f;
    [Tooltip("튕긴 속도가 잦아드는 빠르기. 클수록 빨리 -Z 흐름으로 돌아온다.")]
    public float launchDamping = 4f;

    [Header("Magnet")]
    public float magnetRange = 3f;
    public float magnetMinSpeed = 8f;
    public float magnetMaxSpeed = 25f;
    public float pickupDistance = 0.3f;

    [Header("Value")]
    public int value = 1;

    [Header("Lifetime")]
    public float despawnZ = -15f;

    private Rigidbody rb;
    private Transform player;
    private Transform visualRoot;
    private bool collected = false;

    private float launchVelZ; // 등장 시 +Z로 튕겼다가 0으로 감쇠
    private bool _needsPositionSync = false;   // 스폰 후 첫 물리스텝에서 rb.position 동기화

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        visualRoot = transform.childCount > 0 ? transform.GetChild(0) : transform;
    }

    void OnEnable()
    {
        collected = false;
        if (player == null && PlayerController.PlayerTransform != null)
            player = PlayerController.PlayerTransform;

        launchVelZ = launchSpeed; // 뒤로 튕기며 등장
        _needsPositionSync = true;
    }

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    /// <summary>
    /// 스폰 위치를 즉시 지정. rb.position 직접 대입으로 보간 잔상 방지.
    /// CoinDropper가 Get 직후 호출.
    /// </summary>
    public void Spawn(Vector3 position)
    {
        transform.position = position;
        if (rb != null) rb.position = position;
        _needsPositionSync = false;
    }

    void Update()
    {
        if (!collected)
            visualRoot.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
    }

    void FixedUpdate()
    {
        if (collected) return;

        // 스폰 직후 1회: rb.position을 실제 스폰 위치와 동기화 (옛 위치에서 출발 방지)
        if (_needsPositionSync)
        {
            rb.position = transform.position;
            _needsPositionSync = false;
        }

        Vector3 pos = rb.position;

        // 항상 -Z 컨베이어. 등장 직후엔 +Z 튕김이 더 세서 잠깐 뒤로 갔다가,
        // 그 속도가 사라지면서 자연스럽게 -Z 흐름으로 합류.
        pos.z -= moveSpeedZ * Time.fixedDeltaTime;
        if (launchVelZ > 0f)
        {
            pos.z += launchVelZ * Time.fixedDeltaTime;
            launchVelZ *= Mathf.Exp(-launchDamping * Time.fixedDeltaTime);
            if (launchVelZ < 0.01f) launchVelZ = 0f;
        }

        if (player != null)
        {
            float dist = Vector3.Distance(pos, player.position);

            if (dist <= pickupDistance)
            {
                Collect();
                return;
            }

            float effectiveRange = magnetRange + GlobalMagnetBonus;

            if (dist < effectiveRange)
            {
                float t = 1f - (dist / effectiveRange);
                float magnetSpeed = Mathf.Lerp(magnetMinSpeed, magnetMaxSpeed, t);
                pos = Vector3.MoveTowards(pos, player.position, magnetSpeed * Time.fixedDeltaTime);
            }
        }

        rb.MovePosition(pos);

        if (pos.z < despawnZ) ReturnToPool();
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        if (other.CompareTag("Player")) Collect();
    }

    void Collect()
    {
        if (collected) return;
        collected = true;

        if (CoinManager.Instance != null)
            CoinManager.Instance.AddCoin(value);

        ReturnToPool();
    }

    /// <summary>
    /// 풀이 있으면 반납, 없으면 파괴.
    /// 상태 리셋(collected=false, launchVelZ 등)은 OnEnable에서 처리하므로
    /// 풀에서 다시 꺼낼 때(SetActive(true)) 자동으로 초기화됨.
    /// </summary>
    private void ReturnToPool()
    {
        if (TryGetComponent(out Poolable poolable)) poolable.ReleaseSelf();
        else Destroy(gameObject);
    }
}