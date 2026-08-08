using UnityEngine;

// 임시 스윙 모션(아트 없이 코드 트윈).
// 캐릭터 몸통을 스윙 방향으로 15도 기울였다가(0.1초) 되돌린다(0.15초).
public class SwingMotion : MonoBehaviour
{
    [SerializeField] private AttackExecutor executor; // 비우면 자기/씬에서 찾음
    [Tooltip("기울일 몸통. 비우면 이 오브젝트 — 보통 캡슐 모델 자식")]
    [SerializeField] private Transform body;

    [Header("기울기")]
    [SerializeField] private float tiltAngle = 15f;
    [SerializeField] private float tiltInSeconds = 0.1f;
    [SerializeField] private float tiltOutSeconds = 0.15f;

    private Quaternion baseLocalRot;
    private float t;          // 진행 시간
    private bool playing;

    private void Start()
    {
        if (executor == null) executor = GetComponent<AttackExecutor>();
        if (executor == null) executor = FindObjectOfType<AttackExecutor>();
        if (body == null) body = transform;

        baseLocalRot = body.localRotation;

        if (executor != null) executor.OnSwingStart += Play;
    }

    private void OnDestroy()
    {
        if (executor != null) executor.OnSwingStart -= Play;
    }

    private void Play()
    {
        if (!playing) baseLocalRot = body.localRotation;
        t = 0f;
        playing = true;
    }

    private void Update()
    {
        if (!playing) return;

        t += Time.deltaTime;
        float inT = Mathf.Max(0.01f, tiltInSeconds);
        float outT = Mathf.Max(0.01f, tiltOutSeconds);

        float k;
        if (t <= inT) k = t / inT;                       // 기울임
        else if (t <= inT + outT) k = 1f - (t - inT) / outT; // 복귀
        else
        {
            body.localRotation = baseLocalRot;
            playing = false;
            return;
        }

        // 앞으로 숙이기 (로컬 X축 회전)
        body.localRotation = baseLocalRot * Quaternion.Euler(tiltAngle * k, 0f, 0f);
    }
}
