using System.Text;
using UnityEngine;

// 도끼 제작대. CraftingRecipe 재료를 인벤토리에서 소모하고 PlayerTools에 도끼 지급.
// 시험판 산출물은 도끼뿐 → 이미 도끼가 있으면 상호작용 불가(타겟에서 빠짐).
// 재료가 부족해도 타겟은 되며, 프롬프트에 보유/필요량을 표시.
public class CraftingStation : InteractableBase
{
    [SerializeField] private CraftingRecipe recipe;
    [SerializeField] private Inventory inventory; // 비우면 씬에서 찾음

    private void Start()
    {
        if (inventory == null) inventory = FindObjectOfType<Inventory>();
    }

    public override string Prompt
    {
        get
        {
            if (recipe == null) return "제작대";
            var sb = new StringBuilder();
            sb.Append("제작: ");
            sb.Append(string.IsNullOrEmpty(recipe.outputName) ? "도끼" : recipe.outputName);
            if (recipe.costs != null)
            {
                for (int i = 0; i < recipe.costs.Length; i++)
                {
                    int have = inventory != null ? inventory.Get(recipe.costs[i].kind) : 0;
                    sb.Append($"  [{KindLabel(recipe.costs[i].kind)} {have}/{recipe.costs[i].amount}]");
                }
            }
            return sb.ToString();
        }
    }

    public override bool CanInteract(GameObject interactor)
    {
        if (recipe == null || inventory == null) return false;

        // 이미 도끼가 있으면 만들 필요 없음
        PlayerTools tools = interactor.GetComponent<PlayerTools>();
        if (tools != null && tools.HasAxe) return false;

        return true; // 재료 부족해도 타겟은 되게(프롬프트로 부족 표시)
    }

    public override void Interact(GameObject interactor)
    {
        PlayerTools tools = interactor.GetComponent<PlayerTools>();
        if (tools != null && tools.HasAxe) return;

        if (!HasMaterials())
        {
            Debug.Log("[제작대] 재료 부족");
            return;
        }

        for (int i = 0; i < recipe.costs.Length; i++)
            inventory.TrySpend(recipe.costs[i].kind, recipe.costs[i].amount);

        if (tools != null) tools.GiveAxe();
    }

    private bool HasMaterials()
    {
        if (recipe.costs == null) return true;
        for (int i = 0; i < recipe.costs.Length; i++)
            if (!inventory.Has(recipe.costs[i].kind, recipe.costs[i].amount)) return false;
        return true;
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
