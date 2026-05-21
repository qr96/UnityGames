using UnityEngine;
using AutoBattler.Core;

namespace AutoBattler.Data
{
    [CreateAssetMenu(menuName = "AutoBattler/Hero", fileName = "Hero_")]
    public class HeroData : ScriptableObject
    {
        public string id;
        public string displayName;
        public Sprite portrait;

        [Header("기본 스탯 (영웅 고유, 직업과 합산됨)")]
        public Stats baseStats = new Stats
        {
            attack = 10,
            defense = 2,
            maxHp = 100,
            critRate = 0.1f,
            critDamage = 1.5f,
            attackSpeed = 100,
            attackRange = 0,
            moveSpeed = 1f
        };

        [Header("시작 직업")]
        [Tooltip("보통 Job_Novice. 직업이 무기 외형/모션/추가 수치 결정.")]
        public JobData startingJob;

        [Header("시작 스킬 (옵션)")]
        public SkillData startingSkillA;
        public SkillData startingSkillB;
    }
}
