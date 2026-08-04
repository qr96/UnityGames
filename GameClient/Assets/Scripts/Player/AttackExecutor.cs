using System;
using UnityEngine;

// 공격 실행기. 무기가 늘어도 이 컴포넌트는 그대로다.
// 사용 도구 = 퀵슬롯에서 선택 중이고 보유한 도구. 판정 방식은 그 도구의 AttackPattern이 결정.
// 입력: 좌클릭 = 스윙(커서 방향), 누른 채로 = 쿨다운마다 자동 스윙.
//       인벤토리·상호작용은 키보드 그대로.
public class AttackExecutor : MonoBehaviour
{
    [Header("입력")]
    [Tooltip("스윙에 쓸 마우스 버튼 (0 = 좌클릭)")]
    [SerializeField] private int mouseButton = 0;
    [Tooltip("키보드 테스트용 대체 키. None이면 미사용")]
    [SerializeField] private KeyCode fallbackKey = KeyCode.Space;
    [Tooltip("스윙 간격(초). 홀드 시 이 간격으로 자동 스윙")]
    [SerializeField] private float cooldown = 0.25f;

    [Header("조준")]
    [Tooltip("비우면 Camera.main")]
    [SerializeField] private Camera aimCamera;
    [Tooltip("스윙할 때 캐릭터가 커서 방향을 바라봄")]
    [SerializeField] private bool faceCursorOnSwing = true;

    [SerializeField] private QuickSlotBar quickBar; // 비우면 씬에서 찾음
    [SerializeField] private Inventory inventory;   // 비우면 씬에서 찾음

    [Header("디버그")]
    [SerializeField] private bool verboseLog = true;

    private float nextTime;

    // 공격 동작이 나갈 때마다 발생(대상 유무와 무관) — 연출이 구독
    public event Action OnAttack;

    // 퀵슬롯에서 선택 중이고 실제로 보유한 도구. 없으면 null.
    public ItemDef EquippedTool
    {
        get
        {
            if (quickBar == null || inventory == null) return null;
            ItemDef def = quickBar.SelectedDef;
            if (def == null || !def.IsTool) return null;
            return inventory.Has(def.kind, 1) ? def : null;
        }
    }

    public AttackPattern CurrentPattern
    {
        get
        {
            ItemDef tool = EquippedTool;
            return tool != null ? tool.attackPattern : null;
        }
    }

    // 지금 조준에 걸리는 대상(없으면 null)
    public IHittable CurrentTarget { get; private set; }

    // 커서가 가리키는 방향(XZ 평면). 커서를 못 읽으면 캐릭터 정면.
    public Vector3 AimDirection { get; private set; } = Vector3.forward;

    private void Start()
    {
        if (quickBar == null) quickBar = FindObjectOfType<QuickSlotBar>();
        if (inventory == null) inventory = FindObjectOfType<Inventory>();

        if (aimCamera == null) aimCamera = Camera.main;
        if (quickBar == null) Debug.LogWarning("[공격] QuickSlotBar를 찾지 못함 — 도구 선택 불가");
        if (inventory == null) Debug.LogWarning("[공격] Inventory를 찾지 못함");
    }

    private AttackContext BuildContext(int power)
    {
        return new AttackContext
        {
            attacker = gameObject,
            origin = transform.position,
            forward = AimDirection,
            power = power,
        };
    }

    // 커서를 캐릭터 높이의 수평면에 투사해 방향을 얻는다
    private void UpdateAimDirection()
    {
        if (aimCamera == null) aimCamera = Camera.main;

        Vector3 fallback = transform.forward; fallback.y = 0f;
        Vector3 dir = fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector3.forward;

        if (aimCamera != null)
        {
            Ray ray = aimCamera.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.up, transform.position);
            if (plane.Raycast(ray, out float dist))
            {
                Vector3 point = ray.GetPoint(dist);
                Vector3 d = point - transform.position; d.y = 0f;
                if (d.sqrMagnitude > 0.0001f) dir = d.normalized;
            }
        }

        AimDirection = dir;
    }

    private void Update()
    {
        UpdateAimDirection();

        // 조준 대상 갱신 (커서 방향 기준)
        AttackPattern pattern = CurrentPattern;
        CurrentTarget = pattern != null ? pattern.FindPrimary(BuildContext(1)) : null;

        bool held = Input.GetMouseButton(mouseButton)
                    || (fallbackKey != KeyCode.None && Input.GetKey(fallbackKey));
        if (!held) return;

        if (UIInputLock.IsBlocked)
        {
            if (verboseLog && Input.GetMouseButtonDown(mouseButton))
                Debug.Log("[공격] UI가 열려 있어 입력 무시 (Tab/Q/ESC로 닫기)");
            return;
        }

        if (Time.time < nextTime) return;   // 홀드 중에는 이 간격으로 반복
        nextTime = Time.time + Mathf.Max(0.01f, cooldown);

        if (faceCursorOnSwing && AimDirection.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(AimDirection, Vector3.up);

        DoAttack();
    }

    private void DoAttack()
    {
        ItemDef tool = EquippedTool;
        AttackPattern pattern = tool != null ? tool.attackPattern : null;

        OnAttack?.Invoke(); // 대상이 없어도 동작은 나간다(허공 휘두름)

        if (verboseLog)
        {
            string toolText;
            if (quickBar == null) toolText = "퀵슬롯 없음";
            else if (quickBar.SelectedDef == null) toolText = $"{quickBar.Selected + 1}번 슬롯 비어 있음";
            else if (!quickBar.SelectedDef.IsTool) toolText = $"{quickBar.SelectedDef.displayName}(도구 아님)";
            else if (tool == null) toolText = $"{quickBar.SelectedDef.displayName}(보유 없음)";
            else if (pattern == null) toolText = $"{tool.displayName}(Attack Pattern 미지정)";
            else toolText = $"{tool.displayName} 위력 {tool.hitPower} / {pattern.name}";

            string targetText = CurrentTarget == null ? "없음" : CurrentTarget.Category.ToString();
            Debug.Log($"[공격] 도구: {toolText} / 대상: {targetText}");
        }

        if (pattern == null) return;

        pattern.Execute(BuildContext(Mathf.Max(1, tool.hitPower)));
    }

    private void OnDrawGizmosSelected()
    {
        AttackPattern pattern = Application.isPlaying ? CurrentPattern : null;
        if (pattern != null) pattern.DrawGizmos(transform);
    }
}