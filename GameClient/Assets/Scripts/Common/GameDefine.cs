using UnityEngine;

namespace GameDefine
{
    public class Common
    {
        public static int MaxCardCount = 7;
    }

    public class SkillCardData
    {
        public int skillId;
        public string cardInfo;
        public int rank;

        public SkillCardData(int id)
        {
            skillId = id;
            rank = 1;
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

        // damage : 가한 데미지, realDamage : 실제로 입힌 데미지
        public void OnDamaged(long damage)
        {
            nowStat.hp -= damage;

            if (nowStat.hp < 0)
                nowStat.hp = 0;
        }

        public long GetDamage()
        {
            return nowStat.attack;
        }

        public void Heal(long heal)
        {
            nowStat.hp += heal;

            if (nowStat.hp > originStat.hp)
                nowStat.hp = originStat.hp;
        }

        public bool IsDead()
        {
            return nowStat.hp <= 0;
        }
    }
}
