using UnityEngine;

// 화로 상호작용. 상황에 따라 E가 자동 선택:
//  꺼짐 → 불 피우기(연료 선택) / 연료 여유 + 연료 아이템 보유 → 넣기(연료 선택) / 그 외 강화 가능 → 강화.
// 연료 선택은 FuelSelectUI가 담당. 화로 오브젝트에 Hearth와 함께 붙임.
public class HearthInteractable : InteractableBase
{
    [SerializeField] private Hearth hearth;         // 같은 오브젝트면 자동
    [SerializeField] private Inventory inventory;   // 비우면 씬에서 찾음
    [SerializeField] private FuelSelectUI fuelSelect; // 비우면 씬에서 찾음

    private enum Action { None, Relight, Refuel, Upgrade }

    private void Awake()
    {
        if (hearth == null) hearth = GetComponent<Hearth>();
    }

    private void Start()
    {
        if (inventory == null) inventory = FindObjectOfType<Inventory>();
        if (fuelSelect == null) fuelSelect = FindObjectOfType<FuelSelectUI>();
    }

    // 인벤토리에 연료로 쓸 수 있는 아이템이 하나라도 있는지
    private bool HasAnyFuel()
    {
        if (inventory == null) return false;
        for (int i = 0; i < inventory.SlotCount; i++)
        {
            Inventory.Slot s = inventory.Slots[i];
            if (!s.IsEmpty && s.def.IsFuel) return true;
        }
        return false;
    }

    private Action Decide()
    {
        if (hearth == null || inventory == null) return Action.None;
        if (!hearth.IsLit) return Action.Relight;
        if (hearth.Fuel < hearth.FuelCapacity && HasAnyFuel()) return Action.Refuel;
        if (hearth.CanUpgrade) return Action.Upgrade;
        return Action.None;
    }

    public override string Prompt
    {
        get
        {
            switch (Decide())
            {
                case Action.Relight:
                    return "불 피우기";
                case Action.Refuel:
                    return $"연료 넣기 (연료 {Mathf.FloorToInt(hearth.Fuel)}/{Mathf.FloorToInt(hearth.FuelCapacity)})";
                case Action.Upgrade:
                    if (hearth.TryGetNextUpgrade(out HearthConfig.UpgradeStep step))
                    {
                        string res = KindLabel(step.resourceCost);
                        int haveRes = inventory.Get(step.resourceCost);
                        return $"강화 [골드 {inventory.Gold}/{step.goldCost}] [{res} {haveRes}/{step.resourceAmount}]";
                    }
                    return "강화";
                default:
                    return "화로";
            }
        }
    }

    public override bool CanInteract(GameObject interactor) => Decide() != Action.None;

    public override void Interact(GameObject interactor)
    {
        Action action = Decide();

        if (action == Action.Relight || action == Action.Refuel)
        {
            if (fuelSelect == null) fuelSelect = FindObjectOfType<FuelSelectUI>();
            if (fuelSelect == null)
            {
                Debug.LogWarning("[화로] FuelSelectUI가 씬에 없음");
                return;
            }
            fuelSelect.Open(hearth, inventory, action == Action.Relight);
            return;
        }

        if (action == Action.Upgrade)
        {
            if (!hearth.TryUpgrade(inventory)) Debug.Log("[화로] 강화 골드/자원 부족");
        }
    }

    // 남은 지속 시간 표기 (분:초) — HearthGauge 옵션 표기에서 사용
    public static string FormatTime(float seconds)
    {
        if (float.IsInfinity(seconds)) return "∞";
        int t = Mathf.Max(0, Mathf.FloorToInt(seconds));
        return $"{t / 60}:{t % 60:00}";
    }

    private static string KindLabel(ResourceKind kind)
    {
        switch (kind)
        {
            case ResourceKind.Stick: return "나뭇가지";
            case ResourceKind.Firewood: return "장작";
            case ResourceKind.Food: return "식량";
            case ResourceKind.Axe: return "도끼";
            default: return kind.ToString();
        }
    }
}