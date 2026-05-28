using UnityEngine;

[CreateAssetMenu(menuName = "Runner/Skills/Sword Wave", fileName = "SwordWaveSkill")]
public class SwordWaveSkill : Skill
{
    [Tooltip("한 단계당 추가될 검기 발사 수")]
    public int wavesPerStack = 1;

    public override void Apply(int stackLevel)
    {
        if (PlayerController.Instance != null)
            PlayerController.Instance.AddSwordWave(wavesPerStack);
    }
}
