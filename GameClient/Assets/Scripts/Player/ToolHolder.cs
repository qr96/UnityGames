using UnityEngine;

// 손에 든 도구 표시 + 스윙 모션.
// 핫바에서 든 장비가 도끼면 손 소켓의 막대(도끼 대용)를 켠다.
// 스윙 타이밍: 예비동작(뒤로 젖힘) → 판정 순간에 전방 부채꼴 120도 회전 → 복귀.
public class ToolHolder : MonoBehaviour
{
    [SerializeField] private AttackExecutor attack; // 비우면 자기/씬에서 찾음

    [Header("손")]
    [Tooltip("도구가 붙을 위치(플레이어 자식 트랜스폼)")]
    [SerializeField] private Transform handSocket;
    [Tooltip("손에 들릴 막대/도끼 프리팹")]
    [SerializeField] private GameObject axePrefab;

    [Header("스윙")]
    [Tooltip("휘두르는 총 각도")]
    [SerializeField] private float swingAngle = 120f;
    [Tooltip("예비동작에서 뒤로 젖히는 비율(총 각도 대비)")]
    [SerializeField] private float windupRatio = 0.25f;

    private GameObject axeInstance;

    private enum Phase { Idle, Windup, Swing }
    private Phase phase = Phase.Idle;
    private float t;
    private float windupTime = 0.05f;
    private float swingTime = 0.15f;

    private void Start()
    {
        if (attack == null) attack = GetComponent<AttackExecutor>();
        if (attack == null) attack = FindObjectOfType<AttackExecutor>();

        if (attack != null)
        {
            attack.OnSwingStart += BeginWindup;
            attack.OnSwingHit += BeginSwing;
            windupTime = Mathf.Max(0.01f, attack.WindupSeconds);
            swingTime = Mathf.Max(0.01f, attack.RecoverySeconds);
        }

        if (axePrefab != null && handSocket != null)
        {
            axeInstance = Instantiate(axePrefab, handSocket);
            axeInstance.transform.localPosition = Vector3.zero;
            axeInstance.transform.localRotation = Quaternion.identity;
            axeInstance.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (attack == null) return;
        attack.OnSwingStart -= BeginWindup;
        attack.OnSwingHit -= BeginSwing;
    }

    private void BeginWindup()
    {
        phase = Phase.Windup;
        t = 0f;
    }

    private void BeginSwing(AttackPattern pattern, Vector3 aim)
    {
        phase = Phase.Swing;
        t = 0f;
    }

    private void Update()
    {
        if (axeInstance == null) return;

        bool hold = ShouldHoldAxe();
        axeInstance.SetActive(hold);
        if (!hold) { phase = Phase.Idle; return; }

        // 각도: 예비동작에서 뒤로(-), 판정 이후 앞으로(+) 쓸어내림
        float back = -swingAngle * Mathf.Clamp01(windupRatio);
        float front = swingAngle * (1f - Mathf.Clamp01(windupRatio));
        float angle = 0f;

        switch (phase)
        {
            case Phase.Windup:
                t += Time.deltaTime;
                angle = Mathf.Lerp(0f, back, Mathf.Clamp01(t / windupTime));
                break;

            case Phase.Swing:
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / swingTime);
                angle = Mathf.Lerp(back, front, k);
                if (k >= 1f) { phase = Phase.Idle; angle = 0f; }
                break;
        }

        // 손 소켓 기준 상하 스윙(로컬 X축)
        axeInstance.transform.localRotation = Quaternion.Euler(angle, 0f, 0f);
    }

    private bool ShouldHoldAxe()
    {
        if (attack == null) return false;
        ItemDef tool = attack.EquippedTool;
        return tool != null && tool.toolType == ToolType.Axe;
    }
}