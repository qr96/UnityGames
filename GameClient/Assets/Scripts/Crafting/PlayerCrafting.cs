using UnityEngine;

// 제작 판정·실행. UI는 표시와 선택만 담당한다.
public class PlayerCrafting : MonoBehaviour
{
    [SerializeField] private RecipeBook book;
    [SerializeField] private Inventory inventory; // 비우면 씬에서 찾음

    public RecipeBook Book => book;

    private void Start()
    {
        if (inventory == null) inventory = FindObjectOfType<Inventory>();
        if (book == null) Debug.LogWarning("[제작] Recipe Book이 연결되지 않음");
    }

    // 필요 시설이 근처에 있는지
    public bool HasStation(CraftingRecipe r)
        => r != null && CraftingStation.IsAvailable(r.requiredStation, transform.position);

    public bool HasMaterials(CraftingRecipe r)
    {
        if (r == null || inventory == null) return false;
        if (r.costs == null) return true;
        for (int i = 0; i < r.costs.Length; i++)
            if (!inventory.Has(r.costs[i].kind, r.costs[i].amount)) return false;
        return true;
    }

    public bool HasRoom(CraftingRecipe r)
        => r != null && inventory != null &&
           inventory.FreeSpaceFor(r.outputKind) >= r.outputAmount;

    public bool CanCraft(CraftingRecipe r)
        => HasStation(r) && HasMaterials(r) && HasRoom(r);

    // 제작 불가 사유(표시용). 가능하면 null.
    public string BlockReason(CraftingRecipe r)
    {
        if (r == null) return "레시피 없음";
        if (!HasStation(r)) return $"{CraftingStation.Label(r.requiredStation)} 필요";
        if (!HasMaterials(r)) return "재료 부족";
        if (!HasRoom(r)) return "칸 없음";
        return null;
    }

    public bool TryCraft(CraftingRecipe r)
    {
        string reason = BlockReason(r);
        if (reason != null) { Debug.Log($"[제작] {r?.outputName} — {reason}"); return false; }

        for (int i = 0; i < r.costs.Length; i++)
            inventory.TrySpend(r.costs[i].kind, r.costs[i].amount);

        inventory.Add(r.outputKind, r.outputAmount);
        return true;
    }

    public static string KindLabel(ResourceKind kind)
    {
        switch (kind)
        {
            case ResourceKind.Stick:    return "나뭇가지";
            case ResourceKind.Stone:    return "돌";
            case ResourceKind.Firewood: return "장작";
            case ResourceKind.Food:     return "식량";
            case ResourceKind.Axe:      return "도끼";
            default:                    return kind.ToString();
        }
    }
}
