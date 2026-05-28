using UnityEngine;

[CreateAssetMenu(menuName = "Runner/Skills/Max HP Up", fileName = "MaxHpUpSkill")]
public class MaxHpUpSkill : Skill
{
    [Tooltip("한 단계당 증가할 최대 HP")]
    public int hpBonus = 1;

    public override void Apply(int stackLevel)
    {
        if (PlayerController.Instance != null)
            PlayerController.Instance.AddMaxHP(hpBonus);
    }
}
