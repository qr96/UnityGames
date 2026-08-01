using UnityEngine;

// 제작대. 여러 레시피를 갖고, E를 누르면 CraftingUI 목록을 연다.
// 실제 제작 판정·소모·수납은 이 스크립트가 담당(UI는 표시와 선택만).
public class CraftingStation : InteractableBase
{
    [SerializeField] private CraftingRecipe[] recipes;
    [SerializeField] private Inventory inventory;  // 비우면 씬에서 찾음
    [SerializeField] private CraftingUI craftingUI; // 비우면 씬에서 찾음
    [SerializeField] private string stationName = "제작대";

    public CraftingRecipe[] Recipes => recipes;
    public string StationName => stationName;

    private void Start()
    {
        if (inventory == null) inventory = FindObjectOfType<Inventory>();
        if (craftingUI == null) craftingUI = FindObjectOfType<CraftingUI>();
    }

    public override string Prompt => stationName;

    public override bool CanInteract(GameObject interactor)
        => recipes != null && recipes.Length > 0 && inventory != null;

    public override void Interact(GameObject interactor)
    {
        if (craftingUI == null) craftingUI = FindObjectOfType<CraftingUI>();
        if (craftingUI == null)
        {
            Debug.LogWarning("[제작대] CraftingUI가 씬에 없음");
            return;
        }
        craftingUI.Open(this, inventory);
    }

    public bool HasMaterials(CraftingRecipe recipe)
    {
        if (recipe == null || inventory == null) return false;
        if (recipe.costs == null) return true;
        for (int i = 0; i < recipe.costs.Length; i++)
            if (!inventory.Has(recipe.costs[i].kind, recipe.costs[i].amount)) return false;
        return true;
    }

    public bool HasRoom(CraftingRecipe recipe)
        => recipe != null && inventory != null &&
           inventory.FreeSpaceFor(recipe.outputKind) >= recipe.outputAmount;

    // 1회 제작. 성공 시 true.
    public bool TryCraft(CraftingRecipe recipe)
    {
        if (recipe == null || inventory == null) return false;
        if (!HasMaterials(recipe)) { Debug.Log("[제작대] 재료 부족"); return false; }
        if (!HasRoom(recipe)) { Debug.Log("[제작대] 넣을 칸 없음"); return false; }

        for (int i = 0; i < recipe.costs.Length; i++)
            inventory.TrySpend(recipe.costs[i].kind, recipe.costs[i].amount);

        inventory.Add(recipe.outputKind, recipe.outputAmount);
        return true;
    }

    public static string KindLabel(ResourceKind kind)
    {
        switch (kind)
        {
            case ResourceKind.Stick: return "나뭇가지";
            case ResourceKind.Stone: return "돌";
            case ResourceKind.Firewood: return "장작";
            case ResourceKind.Food: return "식량";
            case ResourceKind.Axe: return "도끼";
            default: return kind.ToString();
        }
    }
}