using System;
using UnityEngine;

// 제작에 필요한 시설 등급. Hand = 맨손(어디서나).
public enum CraftStation
{
    Hand = 0,       // 맨손 — 돌도끼·횃불·모닥불 등
    Workbench = 1,  // 제작대 — 담벼락·오두막·화로·저장고 등
    Anvil = 2,      // 모루(대장장이) — 철제 도구·난로
    CookingPot = 3, // 요리솥
    Fire = 4,       // 불 앞(화로·모닥불)
}

// 제작 레시피. 재료 → 산출물. 필요 시설이 근처에 있어야 제작 가능.
[CreateAssetMenu(fileName = "CraftingRecipe", menuName = "혹한/Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
    public string outputName = "돌도끼";

    [Header("해금")]
    [Tooltip("Hand면 어디서나 제작. 그 외는 해당 시설 근처에서만")]
    public CraftStation requiredStation = CraftStation.Hand;

    [Header("산출")]
    public ResourceKind outputKind = ResourceKind.Axe;
    public int outputAmount = 1;

    [Header("시간")]
    [Tooltip("제작에 걸리는 시간(초). 0이면 즉시")]
    public float craftSeconds = 0f;

    [Header("재료")]
    public Cost[] costs;

    [Serializable]
    public struct Cost
    {
        public ResourceKind kind;
        public int amount;
    }
}