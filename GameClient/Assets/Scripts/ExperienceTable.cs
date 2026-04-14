using UnityEngine;

/// <summary>
/// 레벨별 필요 경험치 테이블.
/// Assets/Data/ExperienceTable.asset 으로 생성해서 PlayerStats에 할당.
/// </summary>
[CreateAssetMenu(fileName = "ExperienceTable", menuName = "RPG/Experience Table")]
public class ExperienceTable : ScriptableObject
{
    [Tooltip("인덱스 0 = Lv1→2에 필요한 XP, 인덱스 49 = Lv50 (미사용)")]
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
        // 46~50: 완만 (엔딩 향해)
        60000, 63000, 66000, 69000, 0  // 50레벨은 최대이므로 0
    };

    /// <summary>레벨 lv → lv+1 에 필요한 XP 반환</summary>
    public int GetRequiredXP(int lv)
    {
        int idx = lv - 1;
        if (idx < 0 || idx >= xpPerLevel.Length) return int.MaxValue;
        return xpPerLevel[idx];
    }
}