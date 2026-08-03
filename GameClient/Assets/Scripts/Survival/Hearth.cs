using System.Collections.Generic;
using UnityEngine;

// 화로. 온기 반경 + 장작 연료(소모→꺼짐→추위 침식) + 불씨 복구 + 강화(용량/반경).
// 정적 API(IsPointWarm/Nearest)는 유지 → PlayerStats 등 기존 코드 호환.
// 상호작용은 HearthInteractable이 담당(이 스크립트는 상태·규칙만).
public class Hearth : MonoBehaviour
{
    public static readonly List<Hearth> All = new List<Hearth>();

    [Header("설정 (있으면 기본값/강화를 여기서 읽음)")]
    [SerializeField] private HearthConfig config;

    [Header("기본값 (config 없을 때)")]
    [SerializeField] private float baseWarmthRadius = 5f;
    [SerializeField] private float baseFuelCapacity = 100f;
    [SerializeField] private float fuelBurnPerSec = 1f;

    [Header("연료")]
    [SerializeField] private float fuel = 100f;
    [SerializeField] private bool isLit = true;

    [Header("비주얼 (선택) — 반경에 맞춰 XZ 스케일(눈 물러남)")]
    [SerializeField] private Transform meltedGroundVisual;

    private int upgradeLevel = 0;
    private float currentRadius;
    private float currentCapacity;
    private float currentBurn;

    public float WarmthRadius => currentRadius;
    public bool IsLit => isLit;
    public float Fuel => fuel;
    public float FuelCapacity => currentCapacity;
    public float FuelRatio => currentCapacity > 0f ? fuel / currentCapacity : 0f;
    public float BurnPerSec => currentBurn;

    // 현재 연료로 남은 지속 시간(초). 소모율이 0이면 무한.
    public float RemainingSeconds => currentBurn > 0f ? fuel / currentBurn : Mathf.Infinity;
    public int UpgradeLevel => upgradeLevel;
    public bool CanUpgrade =>
        config != null && config.upgrades != null && upgradeLevel < config.upgrades.Length;

    private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    private void OnDisable() { All.Remove(this); }

    private void Awake()
    {
        RecalcStats();
        fuel = Mathf.Min(fuel, currentCapacity);
        ApplyRadiusVisual();
    }

    private void Update()
    {
        if (!isLit) return;

        fuel -= currentBurn * Time.deltaTime;
        if (fuel <= 0f)
        {
            fuel = 0f;
            isLit = false; // 꺼짐 → IsPointWarm이 false → 추위 침식
        }
    }

    // ---- 상호작용 진입점 ----

    // 지정한 연료 아이템을 1개 투입. 성공 여부 반환.
    public bool AddFuelUnit(Inventory inv, ItemDef def)
    {
        if (inv == null || def == null || !def.IsFuel) return false;
        if (currentCapacity - fuel < def.fuelValue) return false; // 용량 여유 부족
        if (!inv.Has(def.kind, 1)) return false;

        inv.TrySpend(def.kind, 1);
        fuel = Mathf.Min(currentCapacity, fuel + def.fuelValue);
        return true;
    }

    // 불씨 복구: 지정 연료 1개를 소모하고 점화.
    // (불씨 미니게임은 성공 시 Relight() 호출로 대체 예정)
    public bool RelightWith(Inventory inv, ItemDef def)
    {
        if (isLit) return false;
        if (inv == null || def == null || !def.IsFuel) return false;
        if (!inv.Has(def.kind, 1)) return false;

        inv.TrySpend(def.kind, 1);
        fuel = Mathf.Min(currentCapacity, fuel + def.fuelValue);
        Relight();
        return true;
    }

    // 남은 용량에 이 연료를 몇 개까지 넣을 수 있는지
    public int RoomForUnits(ItemDef def)
    {
        if (def == null || !def.IsFuel) return 0;
        float space = currentCapacity - fuel;
        return space <= 0f ? 0 : Mathf.FloorToInt(space / def.fuelValue);
    }

    public void Relight()
    {
        isLit = true;
    }

    // 강화: 다음 단계 골드+자원 소모 → 용량/반경 증가.
    public bool TryUpgrade(Inventory inv)
    {
        if (!CanUpgrade || inv == null) return false;
        HearthConfig.UpgradeStep step = config.upgrades[upgradeLevel];
        if (inv.Gold < step.goldCost) return false;
        if (!inv.Has(step.resourceCost, step.resourceAmount)) return false;

        inv.TrySpendGold(step.goldCost);
        inv.TrySpend(step.resourceCost, step.resourceAmount);
        upgradeLevel++;
        RecalcStats();
        ApplyRadiusVisual();
        return true;
    }

    public bool TryGetNextUpgrade(out HearthConfig.UpgradeStep step)
    {
        if (CanUpgrade) { step = config.upgrades[upgradeLevel]; return true; }
        step = default;
        return false;
    }

    private void RecalcStats()
    {
        float radius = config != null ? config.baseWarmthRadius : baseWarmthRadius;
        float cap = config != null ? config.baseFuelCapacity : baseFuelCapacity;
        float burn = config != null ? config.fuelBurnPerSec : fuelBurnPerSec;

        if (config != null && config.upgrades != null)
        {
            int n = Mathf.Min(upgradeLevel, config.upgrades.Length);
            for (int i = 0; i < n; i++)
            {
                radius += config.upgrades[i].addedWarmthRadius;
                cap += config.upgrades[i].addedFuelCapacity;
            }
        }

        currentRadius = radius;
        currentCapacity = cap;
        currentBurn = burn;
    }

    private void ApplyRadiusVisual()
    {
        if (meltedGroundVisual == null) return;
        float d = currentRadius * 2f; // 지름
        Vector3 s = meltedGroundVisual.localScale;
        meltedGroundVisual.localScale = new Vector3(d, s.y, d);
    }

    // ---- 정적 조회 ----
    public static bool IsPointWarm(Vector3 point)
    {
        WorldGrid grid = WorldGrid.Instance;

        for (int i = 0; i < All.Count; i++)
        {
            Hearth h = All[i];
            if (!h.isLit) continue;

            Vector3 d = point - h.transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > h.currentRadius * h.currentRadius) continue;

            // 층이 다르면 절벽에 막혀 데워지지 않음(격자 설정으로 전환 가능)
            if (grid != null && !grid.WarmthCrossesLevels)
            {
                int hearthLevel = grid.GetLevel(grid.WorldToCell(h.transform.position));
                int pointLevel = grid.GetLevel(grid.WorldToCell(point));
                if (hearthLevel != pointLevel) continue;
            }

            return true;
        }
        return false;
    }

    public static Hearth Nearest(Vector3 point)
    {
        Hearth best = null;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < All.Count; i++)
        {
            Hearth h = All[i];
            if (!h.isLit) continue;
            Vector3 d = point - h.transform.position;
            d.y = 0f;
            float sqr = d.sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; best = h; }
        }
        return best;
    }

    private void OnDrawGizmosSelected()
    {
        float r = Application.isPlaying
            ? currentRadius
            : (config != null ? config.baseWarmthRadius : baseWarmthRadius);
        Gizmos.color = isLit ? Color.yellow : Color.gray;
        const int seg = 32;
        Vector3 prev = transform.position + new Vector3(r, 0f, 0f);
        for (int i = 1; i <= seg; i++)
        {
            float a = (i / (float)seg) * Mathf.PI * 2f;
            Vector3 p = transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r;
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
    }
}