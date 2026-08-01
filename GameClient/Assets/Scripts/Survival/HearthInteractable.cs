using UnityEngine;

// 화로 상호작용. 상황에 따라 E가 자동 선택:
//  꺼짐 → 불 피우기 / 연료 여유+장작 보유 → 장작 넣기 / 그 외 강화 가능 → 강화.
// 화로 오브젝트에 Hearth와 함께 붙임.
public class HearthInteractable : InteractableBase
{
    [SerializeField] private Hearth hearth;       // 같은 오브젝트면 자동
    [SerializeField] private Inventory inventory; // 비우면 씬에서 찾음

    private enum Action { None, Relight, Refuel, Upgrade }

    private void Awake()
    {
        if (hearth == null) hearth = GetComponent<Hearth>();
    }

    private void Start()
    {
        if (inventory == null) inventory = FindObjectOfType<Inventory>();
    }

    private Action Decide()
    {
        if (hearth == null || inventory == null) return Action.None;
        if (!hearth.IsLit) return Action.Relight;
        if (hearth.Fuel < hearth.FuelCapacity && inventory.Has(ResourceKind.Firewood, 1))
            return Action.Refuel;
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
                    return $"장작 넣기 (연료 {Mathf.FloorToInt(hearth.Fuel)}/{Mathf.FloorToInt(hearth.FuelCapacity)})";
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
        switch (Decide())
        {
            case Action.Relight:
                if (!hearth.TryRelight(inventory)) Debug.Log("[화로] 불씨용 장작 부족");
                break;
            case Action.Refuel:
                hearth.TryRefuel(inventory);
                break;
            case Action.Upgrade:
                if (!hearth.TryUpgrade(inventory)) Debug.Log("[화로] 강화 골드/자원 부족");
                break;
        }
    }

    private static string KindLabel(ResourceKind kind)
    {
        switch (kind)
        {
            case ResourceKind.Stick:    return "나뭇가지";
            case ResourceKind.Firewood: return "장작";
            case ResourceKind.Food:     return "식량";
            default:                    return kind.ToString();
        }
    }
}
