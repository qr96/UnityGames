using UnityEngine;
using AutoBattler.Core;

namespace AutoBattler.Data
{
    [CreateAssetMenu(menuName = "AutoBattler/Weapon", fileName = "Weapon_")]
    public class WeaponData : ScriptableObject
    {
        public string id;
        public string displayName;
        public WeaponType type;

        [Header("스탯 가산")]
        public Stats statBonus;

        [Header("기본 공격")]
        public int baseAttackRange = 1;       // 검 1, 활 5, 지팡이 4 등
        public float baseAttackPowerMul = 1f; // 공격력 배율
        public float projectileSpeed = 12f;   // 원거리만 의미

        // 확장 여지: 기본공격 효과(스플래시/관통 등)는 별도 SO로 분리 권장
    }
}
