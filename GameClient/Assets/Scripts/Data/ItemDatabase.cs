using UnityEngine;

// ResourceKind → ItemDef 조회표. Inventory에 하나 물려서 씀.
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "혹한/Item Database")]
public class ItemDatabase : ScriptableObject
{
    public ItemDef[] items;

    public ItemDef Find(ResourceKind kind)
    {
        if (items == null) return null;
        for (int i = 0; i < items.Length; i++)
            if (items[i] != null && items[i].kind == kind) return items[i];
        return null;
    }
}
