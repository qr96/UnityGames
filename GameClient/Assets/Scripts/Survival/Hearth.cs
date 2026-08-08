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

    [Header("강화 (1회)")]
    [Tooltip("강화에 드는 돌 개수")]
    [SerializeField] private int upgradeStoneCost = 5;
    [Tooltip("강화 후 연료 소모율 배수 (0.66 = 34% 절약)")]
    [SerializeField] private float upgradedBurnMultiplier = 0.66f;
    [SerializeField] private bool isUpgraded = false;

    [Header("비주얼 (선택) — 반경에 맞춰 XZ 스케일(눈 물러남)")]
    [SerializeField] private Transform meltedGroundVisual;

    [Header("강화 비주얼 (선택)")]
    [Tooltip("강화 시 켜질 오브젝트. 없으면 색·크기만 바뀜")]
    [SerializeField] private GameObject upgradedVisual;
    [SerializeField] private Color upgradedTint = new Color(0.55f, 0.75f, 1f);
    [Tooltip("강화 시 곱해질 크기")]
    [SerializeField] private float upgradedScale = 1.2f;

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
    public bool IsUpgraded => isUpgraded;
    public int UpgradeStoneCost => Mathf.Max(1, upgradeStoneCost);
    public bool CanUpgrade => !isUpgraded;
    public string DisplayName => isUpgraded ? "강화 화로" : "화로";

    private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    private void OnDisable() { All.Remove(this); }

    private void Awake()
    {
        RecalcStats();
        fuel = Mathf.Min(fuel, currentCapacity);
        ApplyRadiusVisual();
        ApplyUpgradeVisual();
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

    // 강화(1회): 돌 소모 → 연료 소모율 감소. 성공 시 true.
    public bool TryUpgrade(Inventory inv)
    {
        if (isUpgraded || inv == null) return false;
        if (!inv.Has(ResourceKind.Stone, UpgradeStoneCost)) return false;

        inv.TrySpend(ResourceKind.Stone, UpgradeStoneCost);
        SetUpgraded(true);
        return true;
    }

    // 세이브 복원용 (세이브 시스템 도입 시 이 값을 저장/복원)
    public void LoadUpgraded(bool value) => SetUpgraded(value);

    private void SetUpgraded(bool value)
    {
        isUpgraded = value;
        RecalcStats();
        ApplyUpgradeVisual();
    }

    private void ApplyUpgradeVisual()
    {
        if (upgradedVisual != null)
        {
            upgradedVisual.SetActive(isUpgraded);
            return;
        }

        // 지정 비주얼이 없으면 색·크기로 구분
        Renderer[] rs = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rs.Length; i++)
        {
            if (meltedGroundVisual != null && rs[i].transform.IsChildOf(meltedGroundVisual)) continue;
            Material m = rs[i].material;
            Color c = isUpgraded ? upgradedTint : Color.white;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }

        if (!Mathf.Approximately(upgradedScale, 1f))
        {
            float k = isUpgraded ? upgradedScale : 1f;
            transform.localScale = Vector3.one * k;
        }
    }

    private void RecalcStats()
    {
        currentRadius = config != null ? config.baseWarmthRadius : baseWarmthRadius;
        currentCapacity = config != null ? config.baseFuelCapacity : baseFuelCapacity;

        float burn = config != null ? config.fuelBurnPerSec : fuelBurnPerSec;
        if (isUpgraded) burn *= Mathf.Max(0.01f, upgradedBurnMultiplier);
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