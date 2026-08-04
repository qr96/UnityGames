using UnityEngine;

// 게임 전체 제작 목록. 해금 구조는 각 레시피의 requiredStation이 결정한다.
[CreateAssetMenu(fileName = "RecipeBook", menuName = "혹한/Recipe Book")]
public class RecipeBook : ScriptableObject
{
    public CraftingRecipe[] recipes;
}
