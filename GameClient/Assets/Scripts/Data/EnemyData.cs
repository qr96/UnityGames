using UnityEngine;
using AutoBattler.Core;

namespace AutoBattler.Data
{
    [CreateAssetMenu(menuName = "AutoBattler/Enemy", fileName = "Enemy_")]
    public class EnemyData : ScriptableObject
    {
        public string id;
        public string displayName;

        [Header("스탯 (Lv1 기준)")]
        public Stats baseStats = new Stats {
            attack = 8, defense = 1, maxHp = 60,
            critRate = 0.05f, critDamage = 1.5f,
            attackSpeed = 100, range = 0, moveSpeed = 1f
        };

        [Header("장비/스킬 (옵션)")]
        public WeaponData weapon;
        public SkillData[] skills;

        [Header("프리팹 (옵션)")]
        [Tooltip("비워두면 BattleField의 기본 유닛 프리팹 사용. 풀 키로 별도 적 모델 쓰려면 지정.")]
        public string poolKey;
    }
}
