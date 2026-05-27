using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class Coin : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeedZ = 8f;
    public float rotateSpeed = 180f;

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
    private Transform visualRoot;   // 회전용 (자식이 있으면 그걸 회전, 없으면 자기 자신)
    private bool collected = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // 자식 메시가 있으면 그걸 돌리는 게 안전 (rb를 직접 회전시키지 않기 위해)
        visualRoot = transform.childCount > 0 ? transform.GetChild(0) : transform;
    }

    void OnEnable()
    {
        collected = false;
        if (player == null && PlayerController.PlayerTransform != null)
            player = PlayerController.PlayerTransform;
    }

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    void Update()
    {
        // 시각 효과(회전)는 Update에서. 물리에 영향 없음.
        if (!collected)
            visualRoot.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
    }

    void FixedUpdate()
    {
        if (collected) return;

        Vector3 pos = rb.position;
        pos.z -= moveSpeedZ * Time.fixedDeltaTime;

        if (player != null)
        {
            float dist = Vector3.Distance(pos, player.position);

            if (dist <= pickupDistance)
            {
                Collect();
                return;
            }

            if (dist < magnetRange)
            {
                float t = 1f - (dist / magnetRange);
                float magnetSpeed = Mathf.Lerp(magnetMinSpeed, magnetMaxSpeed, t);
                pos = Vector3.MoveTowards(pos, player.position, magnetSpeed * Time.fixedDeltaTime);
            }
        }

        rb.MovePosition(pos);

        if (pos.z < despawnZ) Destroy(gameObject);
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

        Destroy(gameObject);
    }
}