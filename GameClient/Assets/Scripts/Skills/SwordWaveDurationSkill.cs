using UnityEngine;

[CreateAssetMenu(menuName = "Runner/Skills/Sword Wave Duration", fileName = "SwordWaveDurationSkill")]
public class SwordWaveDurationSkill : Skill
{
    [Tooltip("한 단계당 검기 지속시간에 더해질 초")]
    public float secondsPerStack = 0.5f;

    public override void Apply(int stackLevel)
    {
        if (PlayerController.Instance != null)
            PlayerController.Instance.AddSwordWaveLifeTime(secondsPerStack);
    }
}
