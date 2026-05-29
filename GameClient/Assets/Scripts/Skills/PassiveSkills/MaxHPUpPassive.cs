using UnityEngine;

[CreateAssetMenu(menuName = "Runner/Passives/Max HP Up", fileName = "MaxHPUpPassive")]
public class MaxHPUpPassive : PassiveSkill
{
    [Tooltip("한 단계당 증가할 최대 HP. 현재 HP도 같이 회복됨.")]
    public int hpPerStack = 1;

    public override void Apply(int stackLevel)
    {
        if (PlayerController.Instance != null)
            PlayerController.Instance.AddMaxHP(hpPerStack);
    }
}
