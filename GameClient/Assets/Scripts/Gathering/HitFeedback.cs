using UnityEngine;

// 타격 표시. 맞은 대상이 짧게 흔들리고, 지정하면 파편 프리팹을 뿌린다.
// 사건 가독성용 최소 연출 — 사운드·본격 타격감은 게임필 마디.
public class HitFeedback : MonoBehaviour
{
    [Tooltip("흔들 대상. 비우면 자기 트랜스폼")]
    [SerializeField] private Transform shakeTarget;

    [Header("흔들림")]
    [SerializeField] private float duration = 0.18f;
    [Tooltip("좌우 흔들림 거리")]
    [SerializeField] private float positionAmplitude = 0.08f;
    [Tooltip("기울어지는 각도")]
    [SerializeField] private float angleAmplitude = 5f;
    [Tooltip("흔들리는 왕복 횟수")]
    [SerializeField] private float shakes = 3f;

    [Header("파편 (선택)")]
    [SerializeField] private GameObject debrisPrefab;
    [SerializeField] private Vector3 debrisOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private float debrisLifetime = 1.2f;

    private Vector3 basePos;
    private Quaternion baseRot;
    private float timer;
    private Vector3 shakeAxis = Vector3.right;

    private void Awake()
    {
        if (shakeTarget == null) shakeTarget = transform;
        basePos = shakeTarget.localPosition;
        baseRot = shakeTarget.localRotation;
    }

    // 타격 시 호출. hitDirection은 맞은 방향(없으면 임의).
    public void Play(Vector3 hitDirection)
    {
        if (shakeTarget == null) shakeTarget = transform;

        if (timer <= 0f)
        {
            basePos = shakeTarget.localPosition;
            baseRot = shakeTarget.localRotation;
        }

        hitDirection.y = 0f;
        shakeAxis = hitDirection.sqrMagnitude > 0.0001f
            ? hitDirection.normalized
            : Random.onUnitSphere;
        shakeAxis.y = 0f;

        timer = duration;

        if (debrisPrefab != null)
        {
            GameObject d = Instantiate(debrisPrefab, transform.position + debrisOffset, Random.rotation);
            Destroy(d, debrisLifetime);
        }
    }

    private void Update()
    {
        if (timer <= 0f) return;

        timer = Mathf.Max(0f, timer - Time.deltaTime);
        float t = 1f - (timer / Mathf.Max(0.01f, duration)); // 0~1
        float decay = 1f - t;                                 // 점점 잦아듦
        float wave = Mathf.Sin(t * Mathf.PI * 2f * shakes) * decay;

        shakeTarget.localPosition = basePos + shakeAxis * (positionAmplitude * wave);
        Vector3 tiltAxis = Vector3.Cross(Vector3.up, shakeAxis);
        shakeTarget.localRotation = baseRot * Quaternion.AngleAxis(angleAmplitude * wave, tiltAxis);

        if (timer <= 0f)
        {
            shakeTarget.localPosition = basePos;
            shakeTarget.localRotation = baseRot;
        }
    }
}
