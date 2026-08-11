using System;
using System.Collections;
using UnityEngine;

// 공격 실행기. 무기가 늘어도 이 컴포넌트는 그대로다.
// 사용 도구 = 핫바에서 손에 든 장비. 판정 방식은 그 장비의 AttackPattern이 결정.
// 스페이스 = "손에 든 것 실행" 하나만 의미한다.
// 입력(키보드 전용): 공격 키 = 바라보는 방향 스윙, 누른 채로 = 쿨다운마다 자동 스윙.
//       근접 자동 보정 — 정면 일정 각도 안에 대상이 있으면 그쪽으로 조준을 당겨준다.
public class AttackExecutor : MonoBehaviour
{
    [Header("타이밍")]
    [Tooltip("입력 → 판정까지의 예비동작 시간")]
    [SerializeField] private float windupSeconds = 0.05f;
    [Tooltip("판정 후 복귀 시간(연출용)")]
    [SerializeField] private float recoverySeconds = 0.15f;

    [Header("입력")]
    [SerializeField] private KeyCode attackKey = KeyCode.Space;
    [Tooltip("스윙 간격(초). 홀드 시 이 간격으로 자동 스윙")]
    [SerializeField] private float cooldown = 0.3f;

    [Header("근접 자동 보정")]
    [Tooltip("정면 기준 이 각도 안의 대상으로 조준을 당긴다. 0이면 보정 없음")]
    [Range(0f, 180f)]
    [SerializeField] private float snapAngle = 70f;

    [SerializeField] private Hotbar hotbar;         // 비우면 씬에서 찾음
    [SerializeField] private Inventory inventory;   // 비우면 씬에서 찾음

    [Header("디버그")]
    [SerializeField] private bool verboseLog = true;

    private float nextTime;

    // 입력 순간(예비동작 시작) — 기울기·도구 스윙이 여기서 시작
    public event Action OnSwingStart;
    // 판정 순간 — 궤적 표시가 여기서 뜬다(판정과 시각이 일치)
    public event Action<AttackPattern, Vector3> OnSwingHit;
    // 복귀 완료
    public event Action OnSwingEnd;

    // 차지형 무기: 차지 진행도(0~1) 변화 — 조준 원뿔 표시가 구독
    public event Action<AttackPattern, float> OnCharging;
    // 발사 순간 — origin, 방향, 실제 도달 거리, 무기 사거리, 명중 여부
    public event Action<Vector3, Vector3, float, float, bool> OnRangedShot;

    public bool IsCharging { get; private set; }
    public float Charge01 { get; private set; }

    // 하위 호환: 입력 순간과 동일
    public event Action OnAttack;

    public float WindupSeconds => Mathf.Max(0f, windupSeconds);
    public float RecoverySeconds => Mathf.Max(0f, recoverySeconds);
    public bool IsSwinging { get; private set; }

    // 손에 든 장비. 없으면 null(빈손).
    public ItemDef EquippedTool => hotbar != null ? hotbar.EquippedItem : null;

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
        if (hotbar == null) hotbar = FindObjectOfType<Hotbar>();
        if (inventory == null) inventory = FindObjectOfType<Inventory>();

        if (hotbar == null) Debug.LogWarning("[공격] Hotbar를 찾지 못함 — 장비 사용 불가");
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
            charge01 = 0f,
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

        bool blocked = UIInputLock.IsBlocked;
        bool held = !blocked && Input.GetKey(attackKey);

        // ── 차지형(총·활): 누르는 동안 차지, 떼면 발사 ──
        if (pattern != null && pattern.IsCharged)
        {
            HandleCharged(pattern, held);
            return;
        }

        if (IsCharging) { IsCharging = false; Charge01 = 0f; }

        if (!held)
        {
            if (blocked && verboseLog && Input.GetKeyDown(attackKey))
                Debug.Log("[공격] 창이 열려 있어 입력 무시 (Tab/Q/ESC로 닫기)");
            return;
        }

        if (Time.time < nextTime) return;   // 홀드 중에는 이 간격으로 반복
        nextTime = Time.time + Mathf.Max(0.01f, cooldown);

        DoAttack();
    }

    private void HandleCharged(AttackPattern pattern, bool held)
    {
        if (held)
        {
            IsCharging = true;
            Charge01 = Mathf.Clamp01(Charge01 + Time.deltaTime / Mathf.Max(0.01f, pattern.ChargeSeconds));
            OnCharging?.Invoke(pattern, Charge01);
            return;
        }

        if (!IsCharging) return;   // 누른 적 없음

        // 손을 뗀 순간 발사
        float charge = Charge01;
        IsCharging = false;
        Charge01 = 0f;
        OnCharging?.Invoke(pattern, 0f);

        if (Time.time < nextTime) return;
        nextTime = Time.time + Mathf.Max(0.01f, cooldown);

        Fire(pattern, charge);
    }

    private void Fire(AttackPattern pattern, float charge)
    {
        ItemDef tool = EquippedTool;
        if (tool == null) return;

        // 탄약 소모
        if (tool.consumesAmmo)
        {
            if (inventory == null || tool.ammoItem == null || !inventory.Has(tool.ammoItem, 1))
            {
                if (verboseLog) Debug.Log($"[발사] {tool.displayName}: 탄약 없음");
                return;
            }
            inventory.TrySpend(tool.ammoItem, 1);
        }

        AttackContext ctx = BuildContext(Mathf.Max(1, tool.hitPower));
        ctx.charge01 = charge;

        pattern.Execute(ctx);

        // 연출용 정보 전달
        if (pattern is RangedShotPattern ranged)
        {
            OnRangedShot?.Invoke(ctx.origin, ranged.LastShotDirection,
                                 ranged.LastShotDistance, ranged.LastShotRange,
                                 ranged.LastShotHit);
        }

        OnAttack?.Invoke();

        if (verboseLog)
            Debug.Log($"[발사] {tool.displayName} 차지 {charge:0.00} " +
                      $"→ 명중 {(pattern is RangedShotPattern r2 && r2.LastShotHit ? "O" : "X")}");
    }

    private void DoAttack()
    {
        if (IsSwinging) return;
        StartCoroutine(SwingRoutine());
    }

    private IEnumerator SwingRoutine()
    {
        IsSwinging = true;

        ItemDef tool = EquippedTool;
        AttackPattern pattern = tool != null ? tool.attackPattern : null;

        // 조준 방향은 입력 순간에 고정 (판정과 연출이 같은 방향을 쓰도록)
        Vector3 aim = AimDirection;

        OnSwingStart?.Invoke();
        OnAttack?.Invoke();

        if (verboseLog)
        {
            string toolText;
            if (hotbar == null) toolText = "핫바 없음";
            else if (hotbar.GetAssigned(hotbar.EquippedIndex) == null) toolText = $"{hotbar.EquippedIndex + 1}번 슬롯 비어 있음(빈손)";
            else if (tool == null) toolText = $"{hotbar.GetAssigned(hotbar.EquippedIndex).displayName}(보유 없음)";
            else if (pattern == null) toolText = $"{tool.displayName}(Attack Pattern 미지정)";
            else toolText = $"{tool.displayName} 위력 {tool.hitPower} / {pattern.name}";

            string targetText = CurrentTarget == null ? "없음" : CurrentTarget.Category.ToString();
            Debug.Log($"[공격] 도구: {toolText} / 대상: {targetText}");
        }

        // 예비동작
        if (WindupSeconds > 0f) yield return new WaitForSeconds(WindupSeconds);

        // 판정 + 궤적(같은 프레임)
        if (pattern != null)
        {
            AttackContext ctx = new AttackContext
            {
                attacker = gameObject,
                origin = transform.position,
                forward = aim,
                power = Mathf.Max(1, tool.hitPower),
            };
            pattern.Execute(ctx);
        }
        OnSwingHit?.Invoke(pattern, aim);

        // 복귀
        if (RecoverySeconds > 0f) yield return new WaitForSeconds(RecoverySeconds);

        OnSwingEnd?.Invoke();
        IsSwinging = false;
    }

    private void OnDrawGizmosSelected()
    {
        AttackPattern pattern = Application.isPlaying ? CurrentPattern : null;
        if (pattern != null) pattern.DrawGizmos(transform);
    }
}