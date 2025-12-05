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
        public long attack;
        public long defense;
    }

    public class PlayerData
    {
        public long money { get; set; }
        public int weaponSlot {  get; set; }
    }

    public class ItemData
    {
        public int itemCode { get; set; }
        public int count { get; set; }
    }

    public class PlayerModel : UnitModel
    {
        public long Exp;
        public long Gold;
        public long NowMp { get; private set; }
        public long MaxMp { get; set; }

        public PlayerModel(string name, Stat stat) : base(name, stat)
        {
            // base
        }

        public void Respawn()
        {
            Exp = 0;
            Gold = 0;
            Level = 1;
            NowMp = MaxMp;
            Reset();
        }

        public void GainMp(long mp)
        {
            NowMp += mp;
            if (NowMp > MaxMp)
                NowMp = MaxMp;
        }

        public void ReduceMp(long mp)
        {
            NowMp -= mp;
            if (NowMp < 0)
                NowMp = 0;
        }
    }

    public class UnitModel
    {
        public string Name { get; private set; }
        public int Level;

        public Stat MaxStat => maxStat;
        public Stat NowStat => nowStat;

        Stat maxStat;
        Stat nowStat;

        public UnitModel(string name, Stat stat)
        {
            this.Name = name;
            maxStat = stat;
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
