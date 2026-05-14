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

        [Header("기본 스탯 (Lv1)")]
        public Stats baseStats = new Stats {
            attack = 10, defense = 2, maxHp = 100,
            critRate = 0.1f, critDamage = 1.5f,
            attackSpeed = 100, range = 1, moveSpeed = 1f
        };

        [Header("시작 장비/스킬")]
        public WeaponData startingWeapon;
        public SkillData startingSkillA;
        public SkillData startingSkillB; // 액티브 2슬롯 — 스킬 교체 불가가 원칙

        [Header("전직 분기 (전직북 사용 시)")]
        public HeroData[] advancementBranches;
    }
}
