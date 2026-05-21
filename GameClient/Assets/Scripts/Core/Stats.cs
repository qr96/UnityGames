using System;
using UnityEngine;

namespace AutoBattler.Core
{
    /// <summary>
    /// 기본 설계의 5스탯 + 전투 보조 스탯
    /// 합산 가능하도록 struct + operator 제공
    /// </summary>
    [Serializable]
    public struct Stats
    {
        public float attack;        // 공격력
        public float defense;       // 방어력
        public float maxHp;         // 최대 체력
        [Range(0, 1)] public float critRate;  // 치명타 확률 (0~1)
        public float critDamage;    // 치명타 피해 배율 (1.5 = 150%)

        // 전투 보조
        public float attackSpeed;   // 100 = 1초에 1번
        public int attackRange;     // 공격 사거리 보너스(칸). 0이 기본. 무기 사거리에 합산됨.
        public float moveSpeed;     // 칸/초 (기본 1.0)

        public static Stats Zero => new Stats();

        public static Stats operator +(Stats a, Stats b) => new Stats
        {
            attack = a.attack + b.attack,
            defense = a.defense + b.defense,
            maxHp = a.maxHp + b.maxHp,
            critRate = Mathf.Clamp01(a.critRate + b.critRate),
            critDamage = a.critDamage + b.critDamage,
            attackSpeed = a.attackSpeed + b.attackSpeed,
            attackRange = a.attackRange + b.attackRange,
            moveSpeed = a.moveSpeed + b.moveSpeed,
        };

        public float AttackInterval =>
            attackSpeed <= 0f ? 99f : 100f / attackSpeed;
    }
}