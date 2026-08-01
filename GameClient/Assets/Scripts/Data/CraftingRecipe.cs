using System;
using UnityEngine;

// 제작 레시피 (시험판: 도끼 = 나뭇가지 N개).
[CreateAssetMenu(fileName = "CraftingRecipe", menuName = "혹한/Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
    public string outputName;   // 예: 도끼
    public Cost[] costs;

    [Serializable]
    public struct Cost
    {
        public ResourceKind kind;
        public int amount;
    }
}
