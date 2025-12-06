using GameData;
using UnityEngine;

public class StatBalancer
{
    // === 몬스터 능력치 곡선 계수 (상수) ===
    // HP: A*L^2 + B*L + C
    private const float MonsterHpA = 0.8f;
    private const float MonsterHpB = 5.0f;
    private const float MonsterHpC = 150.0f;

    // DEF: A*L^2 + B*L + C
    private const float MonsterDefA = 0.1f;
    private const float MonsterDefB = 2.0f;
    private const float MonsterDefC = 10.0f;

    // ATK: A*L^2 + B*L + C
    private const float MonsterAtkA = 0.8f;
    private const float MonsterAtkB = 6.0f;
    private const float MonsterAtkC = 50.0f;

    // === 플레이어 레벨당 증가량 (상수) ===
    private const int PlayerHpPerLevel = 15;
    private const int PlayerDefPerLevel = 2;
    private const int PlayerAtkPerLevel = 30;

    // === 경험치 곡선 계수 ===
    private const float MonsterExpA = 0.5f;
    private const float MonsterExpB = 10.0f;
    private const float MonsterExpC = 50.0f;

    // === 골드 곡선 계수 ===
    private const float MonsterGoldA = 2.0f;
    private const float MonsterGoldB = 7.0f;
    private const float MonsterGoldC = 20.0f;

    // === 플레이어 필요 경험치 곡선 계수 ===
    private const float PlayerExpReqA = 2.5f;
    private const float PlayerExpReqB = 10.0f;
    private const float PlayerExpReqC = 137.5f;

    /// <summary>
    /// 몬스터의 레벨에 따른 모든 능력치를 계산하여 반환합니다.
    /// </summary>
    public static Stat GetMonsterStatsByLevel(int level)
    {
        // 2차 다항 함수 계산
        float l_sq = level * level;

        return new Stat()
        {
            hp = (int)(MonsterHpA * l_sq + MonsterHpB * level + MonsterHpC),
            defense = (int)(MonsterDefA * l_sq + MonsterDefB * level + MonsterDefC),
            attack = (int)(MonsterAtkA * l_sq + MonsterAtkB * level + MonsterAtkC)
        };
    }

    /// <summary>
    /// 플레이어의 레벨에 따른 능력치 증가량을 계산합니다.
    /// </summary>
    public static Stat GetPlayerStatsByLevel(int level, Stat baseStats)
    {
        int levelDifference = level - 1; // 1레벨부터 시작한다고 가정

        return new Stat
        {
            hp = baseStats.hp + (PlayerHpPerLevel * levelDifference),
            defense = baseStats.defense + (PlayerDefPerLevel * levelDifference),
            attack = baseStats.attack + (PlayerAtkPerLevel * levelDifference)
        };
    }

    /// <summary>
    /// 몬스터의 레벨에 따른 기본 경험치 보상을 계산합니다.
    /// </summary>
    public static int GetMonsterExpByLevel(int level)
    {
        float l_sq = level * level;
        return (int)(MonsterExpA * l_sq + MonsterExpB * level + MonsterExpC);
    }

    /// <summary>
    /// 몬스터의 레벨에 따른 기본 골드 보상을 계산합니다.
    /// </summary>
    public static int GetMonsterGoldByLevel(int level)
    {
        float l_sq = level * level;
        return (int)(MonsterGoldA * l_sq + MonsterGoldB * level + MonsterGoldC);
    }

    /// <summary>
    /// 플레이어 레벨에서 다음 레벨로 가기 위해 필요한 총 경험치를 계산합니다.
    /// </summary>
    public static int GetPlayerExpRequired(int level)
    {
        // 1레벨에서 다음 레벨(2레벨)로 가기 위해 필요한 EXP
        if (level < 1) return 0;

        float l_sq = level * level;
        return (int)(PlayerExpReqA * l_sq + PlayerExpReqB * level + PlayerExpReqC);
    }
}
