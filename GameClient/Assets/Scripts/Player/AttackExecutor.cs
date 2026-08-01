using System;
using UnityEngine;

// 공격 실행기. 무기가 늘어도 이 컴포넌트는 그대로다.
// 사용 도구 = 퀵슬롯에서 선택 중이고 보유한 도구. 판정 방식은 그 도구의 AttackPattern이 결정.
public class AttackExecutor : MonoBehaviour
{
    [SerializeField] private KeyCode attackKey = KeyCode.Space;
    [Tooltip("연타 제한(초). 0이면 제한 없음")]
    [SerializeField] private float cooldown = 0.25f;

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

    private void Start()
    {
        if (quickBar == null) quickBar = FindObjectOfType<QuickSlotBar>();
        if (inventory == null) inventory = FindObjectOfType<Inventory>();

        if (quickBar == null)  Debug.LogWarning("[공격] QuickSlotBar를 찾지 못함 — 도구 선택 불가");
        if (inventory == null) Debug.LogWarning("[공격] Inventory를 찾지 못함");
    }

    private AttackContext BuildContext(int power)
    {
        Vector3 f = transform.forward; f.y = 0f;
        return new AttackContext
        {
            attacker = gameObject,
            origin = transform.position,
            forward = f.sqrMagnitude > 0.0001f ? f.normalized : Vector3.forward,
            power = power,
        };
    }

    private void Update()
    {
        // 조준 대상 갱신
        AttackPattern pattern = CurrentPattern;
        CurrentTarget = pattern != null ? pattern.FindPrimary(BuildContext(1)) : null;

        if (!Input.GetKeyDown(attackKey)) return;

        if (UIInputLock.IsBlocked)
        {
            if (verboseLog) Debug.Log("[공격] UI가 열려 있어 입력 무시 (Tab/Q/ESC로 닫기)");
            return;
        }
        if (Time.time < nextTime) return;

        nextTime = Time.time + Mathf.Max(0f, cooldown);
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
