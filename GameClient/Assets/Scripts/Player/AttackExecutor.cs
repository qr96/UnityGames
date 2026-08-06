using System;
using UnityEngine;

// 공격 실행기. 무기가 늘어도 이 컴포넌트는 그대로다.
// 사용 도구 = 퀵슬롯에서 선택 중이고 보유한 도구. 판정 방식은 그 도구의 AttackPattern이 결정.
// 입력(키보드 전용): 공격 키 = 바라보는 방향 스윙, 누른 채로 = 쿨다운마다 자동 스윙.
//       근접 자동 보정 — 정면 일정 각도 안에 대상이 있으면 그쪽으로 조준을 당겨준다.
public class AttackExecutor : MonoBehaviour
{
    [Header("입력")]
    [SerializeField] private KeyCode attackKey = KeyCode.Space;
    [Tooltip("스윙 간격(초). 홀드 시 이 간격으로 자동 스윙")]
    [SerializeField] private float cooldown = 0.3f;

    [Header("근접 자동 보정")]
    [Tooltip("정면 기준 이 각도 안의 대상으로 조준을 당긴다. 0이면 보정 없음")]
    [Range(0f, 180f)]
    [SerializeField] private float snapAngle = 70f;

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

    // 조준 방향(XZ 평면) = 캐릭터 정면, 근처 대상이 있으면 그쪽으로 보정된 방향.
    public Vector3 AimDirection { get; private set; } = Vector3.forward;

    private void Start()
    {
        if (quickBar == null) quickBar = FindObjectOfType<QuickSlotBar>();
        if (inventory == null) inventory = FindObjectOfType<Inventory>();

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

    // 정면을 기준으로, 자동 보정 각도 안의 가장 가까운 대상으로 조준을 당긴다
    private void UpdateAimDirection()
    {
        Vector3 f = transform.forward; f.y = 0f;
        Vector3 dir = f.sqrMagnitude > 0.0001f ? f.normalized : Vector3.forward;

        AttackPattern pattern = CurrentPattern;
        if (pattern == null || snapAngle <= 0f) { AimDirection = dir; return; }

        // 정면으로 한 번 찾아보고, 없으면 보정 각도 안에서 다시 찾는다
        AttackContext probe = new AttackContext
        {
            attacker = gameObject,
            origin = transform.position,
            forward = dir,
            power = 1,
        };

        IHittable direct = pattern.FindPrimary(probe);
        if (direct != null) { AimDirection = dir; return; }

        IHittable snapped = FindSnapTarget(pattern, dir);
        if (snapped != null)
        {
            Vector3 d = snapped.HitPosition - transform.position; d.y = 0f;
            if (d.sqrMagnitude > 0.0001f) dir = d.normalized;
        }

        AimDirection = dir;
    }

    // 보정 각도 안에서 패턴이 맞힐 수 있는 가장 가까운 대상
    private IHittable FindSnapTarget(AttackPattern pattern, Vector3 facing)
    {
        float cosLimit = Mathf.Cos(snapAngle * 0.5f * Mathf.Deg2Rad);
        IHittable best = null;
        float bestSqr = float.MaxValue;

        var list = HittableRegistry.All;
        for (int i = 0; i < list.Count; i++)
        {
            IHittable h = list[i];
            if (h == null || !h.CanBeHit || !pattern.CanHit(h.Category)) continue;

            Vector3 d = h.HitPosition - transform.position; d.y = 0f;
            float sqr = d.sqrMagnitude;
            if (sqr > bestSqr || sqr < 0.0001f) continue;
            if (Vector3.Dot(facing, d.normalized) < cosLimit) continue;

            // 그 방향으로 조준했을 때 실제로 맞는지 확인
            AttackContext ctx = new AttackContext
            {
                attacker = gameObject,
                origin = transform.position,
                forward = d.normalized,
                power = 1,
            };
            if (pattern.FindPrimary(ctx) == null) continue;

            bestSqr = sqr;
            best = h;
        }
        return best;
    }

    private void Update()
    {
        UpdateAimDirection();

        // 조준 대상 갱신 (커서 방향 기준)
        AttackPattern pattern = CurrentPattern;
        CurrentTarget = pattern != null ? pattern.FindPrimary(BuildContext(1)) : null;

        if (!Input.GetKey(attackKey)) return;

        if (UIInputLock.IsBlocked)
        {
            if (verboseLog && Input.GetKeyDown(attackKey))
                Debug.Log("[공격] UI가 열려 있어 입력 무시 (Tab/Q/ESC로 닫기)");
            return;
        }

        if (Time.time < nextTime) return;   // 홀드 중에는 이 간격으로 반복
        nextTime = Time.time + Mathf.Max(0.01f, cooldown);

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