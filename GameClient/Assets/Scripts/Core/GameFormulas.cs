using UnityEngine;

/// <summary>
/// 게임 내 수식 모음. 상태 없는 순수 static 클래스.
/// 수식 변경은 여기만 건드리면 전체 반영.
/// </summary>
public static class GameFormulas
{
    // ── 데미지 ────────────────────────────────────────────────────────────

    /// <summary>방어력 적용 후 실제 데미지. 최소 1 보장.</summary>
    public static int GetActualDamage(int rawDamage, int defense)
    {
        return Mathf.Max(1, rawDamage - defense);
    }

    // ── 경험치 ────────────────────────────────────────────────────────────

    /// <summary>
    /// 적 처치 시 지급할 XP.
    /// GDD: 일반 = monsterLevel × baseXP / 엘리트 ×3 / 보스 ×10
    /// 레벨 보정: 플레이어보다 5레벨 이상 강함 → ×1.5 / 약함 → ×0.3
    /// </summary>
    public static int GetXPReward(EnemyData data, int playerLevel)
    {
        float xp = data.baseXP * data.level;

        xp *= data.enemyType switch
        {
            EnemyType.Elite => 3f,
            EnemyType.Boss => 10f,
            _ => 1f
        };

        int levelDiff = data.level - playerLevel;
        if (levelDiff >= 5) xp *= 1.5f;
        else if (levelDiff <= -5) xp *= 0.3f;

        return Mathf.Max(1, Mathf.RoundToInt(xp));
    }
}
