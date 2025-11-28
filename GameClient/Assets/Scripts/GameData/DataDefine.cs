using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameData
{
    public struct Stat
    {
        public long hp;
        public long mp;
        public long attack;
        public long defense;
    }

    public class PlayerData
    {
        public long money { get; set; }
    }

    public class ItemData
    {
        public int itemCode { get; set; }
        public int count { get; set; }
    }

    public class UnitModel
    {
        public string name { get; private set; }
        public int Level;
        public long Exp;
        public Stat MaxStat => maxStat;
        public Stat NowStat => nowStat;

        Stat maxStat;
        Stat nowStat;

        public UnitModel(string name, Stat stat)
        {
            this.name = name;
            maxStat = stat;
            Level = 1;
        }

        public void Reset()
        {
            nowStat = maxStat;
        }

        public void SetStat(Stat stat)
        {
            maxStat = stat;
        }

        public void ReduceHp(long hp)
        {
            nowStat.hp -= hp;
            if (nowStat.hp < 0)
                nowStat.hp = 0;
        }

        public void ReduceMp(long mp)
        {
            nowStat.mp -= mp;
            if (nowStat.mp < 0)
                nowStat.mp = 0;
        }

        public void TakeDamage(long damage)
        {
            ReduceHp(damage);
        }

        public void HealHp(long hp)
        {
            nowStat.hp += hp;
            if (nowStat.hp > maxStat.hp)
                nowStat.hp = maxStat.hp;
        }

        public void HealMp(long mp)
        {
            nowStat.mp += mp;
            if (nowStat.mp > maxStat.mp)
                nowStat.mp = maxStat.mp;
        }

        public long GetAttack()
        {
            return nowStat.attack;
        }

        public long GetDefense()
        {
            return nowStat.defense;
        }

        public bool IsDead()
        {
            return nowStat.hp <= 0;
        }
    }
}
