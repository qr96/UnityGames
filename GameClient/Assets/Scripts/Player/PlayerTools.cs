using UnityEngine;

// 도구 보유·선택. 도구는 인벤토리 아이템으로 존재하며,
// 같은 종류 중 위력(hitPower)이 가장 높은 것이 자동으로 쓰인다(문맥 자동 장착, 스왑 없음).
public class PlayerTools : MonoBehaviour
{
    [SerializeField] private Inventory inventory; // 비우면 씬에서 찾음

    private void Start()
    {
        if (inventory == null) inventory = FindObjectOfType<Inventory>();
    }

    // 보유 중인 해당 종류 도구 가운데 위력이 가장 높은 것. 없으면 null.
    public ItemDef GetBestTool(ToolType type)
    {
        if (inventory == null) inventory = FindObjectOfType<Inventory>();
        if (inventory == null || type == ToolType.None) return null;

        ItemDef best = null;
        for (int i = 0; i < inventory.SlotCount; i++)
        {
            Inventory.Slot s = inventory.Slots[i];
            if (s.IsEmpty) continue;
            if (s.def.toolType != type) continue;
            if (best == null || s.def.hitPower > best.hitPower) best = s.def;
        }
        return best;
    }

    public ItemDef CurrentAxe => GetBestTool(ToolType.Axe);
    public bool HasAxe => CurrentAxe != null;
}