using UnityEngine;

[CreateAssetMenu(menuName = "Runner/Skills/Fire Rate Up", fileName = "FireRateUpSkill")]
public class FireRateUpSkill : Skill
{
    [Tooltip("발사 간격에 곱해지는 값 (0.85 = 15% 빨라짐)")]
    public float intervalMultiplier = 0.85f;

    public override void Apply(int stackLevel)
    {
        if (PlayerController.Instance != null)
            PlayerController.Instance.ModifyFireInterval(intervalMultiplier);
    }
}