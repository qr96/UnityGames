using UnityEngine;

/// <summary>
/// 플레이어 성장 관련 정적 수치.
/// 수치 조정은 이 ScriptableObject 하나만 수정하면 전체 반영.
/// Assets/Data/CharacterGrowthData.asset 으로 생성 후 PlayerStats에 할당.
/// </summary>
[CreateAssetMenu(fileName = "CharacterGrowthData", menuName = "RPG/Character Growth Data")]
public class CharacterGrowthData : ScriptableObject
{
    [Header("레벨 상한")]
    public int maxLevel = 50;

    [Header("초기 스탯 (1레벨 기준)")]
    public int baseHP = 50;
    public int baseAttack = 10;
    public int baseDefense = 0;
    public float baseCritDamage = 1.5f; // 150%

    [Header("레벨업당 스탯 상승")]
    public int hpPerLevel = 10;
    public int attackPerLevel = 2;
    public int defensePerLevel = 1;

    [Header("경험치 테이블")]
    [Tooltip("인덱스 0 = Lv1→2 필요 XP. 마지막 요소는 최대레벨이므로 0.")]
    public int[] xpPerLevel = new int[50]
    {
        // 1~10: 빠른 성장 (튜토리얼)
        120, 160, 200, 260, 320, 400, 480, 560, 660, 780,
        // 11~20: 가파름 (첫 번째 도전)
        1100, 1300, 1550, 1850, 2200, 2600, 3050, 3550, 4100, 4700,
        // 21~25: 완만 (숨 고르기)
        5000, 5300, 5600, 5900, 6200,
        // 26~35: 가파름 (두 번째 도전)
        7000, 8000, 9200, 10600, 12200, 14000, 16000, 18200, 20600, 23200,
        // 36~40: 완만 (숨 고르기)
        24500, 25800, 27100, 28400, 29700,
        // 41~45: 매우 가파름 (마지막 도전)
        34000, 39000, 44500, 50500, 57000,
        // 46~49: 완만, 50레벨은 최대이므로 0
        60000, 63000, 66000, 69000, 0
    };

    public int GetMaxHP(int level) => baseHP + (level - 1) * hpPerLevel;
    public int GetAttack(int level) => baseAttack + (level - 1) * attackPerLevel;
    public int GetDefense(int level) => baseDefense + (level - 1) * defensePerLevel;

    /// <summary>level → level+1 에 필요한 XP.</summary>
    public int GetRequiredXP(int level)
    {
        int idx = level - 1;
        if (idx < 0 || idx >= xpPerLevel.Length) return int.MaxValue;
        return xpPerLevel[idx];
    }
}
