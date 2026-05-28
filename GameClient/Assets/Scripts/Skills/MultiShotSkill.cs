using UnityEngine;

[CreateAssetMenu(menuName = "Runner/Skills/Multi Shot", fileName = "MultiShotSkill")]
public class MultiShotSkill : Skill
{
    [Tooltip("한 단계당 추가될 탄환 수")]
    public int projectilesPerStack = 1;

    public override void Apply(int stackLevel)
    {
        if (PlayerController.Instance != null)
            PlayerController.Instance.AddProjectileCount(projectilesPerStack);
    }
}