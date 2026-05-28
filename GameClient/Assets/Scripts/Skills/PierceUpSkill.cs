using UnityEngine;

[CreateAssetMenu(menuName = "Runner/Skills/Pierce Up", fileName = "PierceUpSkill")]
public class PierceUpSkill : Skill
{
    [Tooltip("각 스택 레벨에서 총 보너스 관통 수. 인덱스 0 = 1스택. 예: [1,2,4,7] = 1스택 +1, 2스택 +2, 3스택 +4, 4스택 +7")]
    public int[] bonusByStack = new int[] { 1, 2, 3, 4, 5 };

    public override void Apply(int stackLevel)
    {
        if (PlayerController.Instance == null) return;

        // 누적이 아니라 "총 보너스를 이 값으로 설정"하는 방식
        int targetBonus = GetBonusForStack(stackLevel);
        int delta = targetBonus - GetBonusForStack(stackLevel - 1);

        PlayerController.Instance.AddBonusPierce(delta);
    }

    int GetBonusForStack(int stack)
    {
        if (stack <= 0) return 0;
        int idx = Mathf.Min(stack - 1, bonusByStack.Length - 1);
        return bonusByStack[idx];
    }
}