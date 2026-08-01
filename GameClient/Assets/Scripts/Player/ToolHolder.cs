using UnityEngine;

// 손에 든 도구 표시 + 휘두름 표시.
// 퀵슬롯에서 도끼를 선택하고 실제로 보유 중이면 손의 도끼 모델을 켠다.
// AttackExecutor.OnAttack에 맞춰 짧게 휘두르는 동작을 재생(사건 가독성용 최소 연출).
public class ToolHolder : MonoBehaviour
{
    [SerializeField] private AttackExecutor attack;  // 비우면 자기/씬에서 찾음

    [Header("손")]
    [Tooltip("도구가 붙을 위치(플레이어 자식 트랜스폼)")]
    [SerializeField] private Transform handSocket;
    [Tooltip("손에 들릴 도끼 모델 프리팹")]
    [SerializeField] private GameObject axePrefab;

    [Header("휘두름 표시")]
    [SerializeField] private float swingDuration = 0.22f;
    [Tooltip("휘두를 때 도구가 회전하는 각도")]
    [SerializeField] private float swingAngle = 80f;

    private GameObject axeInstance;
    private float swingTimer;

    private void Start()
    {
        if (attack == null) attack = GetComponent<AttackExecutor>();
        if (attack == null) attack = FindObjectOfType<AttackExecutor>();
        if (attack != null) attack.OnAttack += PlaySwing;

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
        if (attack != null) attack.OnAttack -= PlaySwing;
    }

    private void PlaySwing() => swingTimer = swingDuration;

    private void Update()
    {
        if (axeInstance == null) return;

        bool hold = ShouldHoldAxe();
        axeInstance.SetActive(hold);
        if (!hold) { swingTimer = 0f; return; }

        // 0 → 1 → 0 으로 왕복하는 짧은 회전
        if (swingTimer > 0f)
        {
            swingTimer = Mathf.Max(0f, swingTimer - Time.deltaTime);
            float t = 1f - (swingTimer / Mathf.Max(0.01f, swingDuration)); // 0~1 진행
            float k = Mathf.Sin(t * Mathf.PI);                              // 0→1→0
            axeInstance.transform.localRotation = Quaternion.Euler(-swingAngle * k, 0f, 0f);
        }
        else
        {
            axeInstance.transform.localRotation = Quaternion.identity;
        }
    }

    // 퀵슬롯에서 선택한 도구가 도끼면 손에 들린다.
    private bool ShouldHoldAxe()
    {
        if (attack == null) return false;
        ItemDef tool = attack.EquippedTool;
        return tool != null && tool.toolType == ToolType.Axe;
    }
}