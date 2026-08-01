using System;
using UnityEngine;

// 제작 레시피. 재료 → 산출물.
[CreateAssetMenu(fileName = "CraftingRecipe", menuName = "혹한/Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
    public string outputName = "도끼";

    [Header("산출")]
    public ResourceKind outputKind = ResourceKind.Axe;
    public int outputAmount = 1;

    [Header("재료")]
    public Cost[] costs;

    [Serializable]
    public struct Cost
    {
        public ResourceKind kind;
        public int amount;
    }
}