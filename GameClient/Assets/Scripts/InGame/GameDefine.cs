using UnityEngine;


public class Common
{
    public static int MaxSlotCount = 5;
    public static int ElementCount = 5; // 원소 개수 (화염, 빙결, 대지, 전격, 바람)

    public enum ElementType
    {
        None = 0,
        Fire = 1,
        Ice = 2,
        Earth = 3,
        Thunder = 4,
        Wind = 5
    }

    public enum ScrollType
    {
        None = 0,
        StandardFire = 1,
        StandardIce = 2,
        StandardEarth = 3,
        StandardThunder = 4,
        StandardWind = 5,
        
    }
}

public class GameUtil
{
    // 속성 슬롯에서 특정 속성 개수 반환
    public static int GetElementCount(Common.ElementType[] elements, Common.ElementType targetElement)
    {
        var elementCount = 0;

        foreach (var element in elements)
        {
            if (element == targetElement)
                elementCount++;
        }

        return elementCount;
    }
}

public struct Stat
{
    public long hp; // 최대 체력
    public long attack; // 공격력
}

public class BaseUnit
{
    public Stat originStat { get; private set; }
    public Stat nowStat;

    public BaseUnit(Stat stat)
    {
        originStat = stat;
    }

    public void Spawn()
    {
        nowStat = originStat;
    }

    public void OnDamaged(long damage)
    {
        nowStat.hp -= damage;
        if (nowStat.hp < 0)
            nowStat.hp = 0;
    }
}

