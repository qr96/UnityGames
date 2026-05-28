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

            float effectiveRange = magnetRange + GlobalMagnetBonus;

            if (dist < effectiveRange)
            {
                float t = 1f - (dist / effectiveRange);
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