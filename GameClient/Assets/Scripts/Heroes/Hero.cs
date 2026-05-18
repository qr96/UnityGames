using UnityEngine;
using AutoBattler.Core;
using AutoBattler.Data;

namespace AutoBattler.Heroes
{
    /// <summary>
    /// 전투 외부에서 들고 다니는 영웅 인스턴스.
    /// 전투 진입 시 BattleUnit이 이 정보를 받아 초기화한다.
    ///
    /// 무기는 영웅이 직접 보유 (직업 시스템 없음).
    /// 스킬은 인벤토리에서 장착/교체 가능.
    /// </summary>
    [System.Serializable]
    public class Hero
    {
        public HeroData data;
        public WeaponData weapon;

        // 액티브 슬롯 2개 — 인벤토리에서 교체 가능
        public SkillData skillA;
        public SkillData skillB;

        public Hero(HeroData baseData)
        {
            data = baseData;
            weapon = baseData != null ? baseData.startingWeapon : null;
            skillA = baseData != null ? baseData.startingSkillA : null;
            skillB = baseData != null ? baseData.startingSkillB : null;
        }

        public Stats GetFinalStats()
        {
            var s = data != null ? data.baseStats : Stats.Zero;
            if (weapon != null) s = s + weapon.statBonus;
            return s;
        }

        public int GetBaseAttackRange()
        {
            int wr = weapon != null ? weapon.baseAttackRange : 1;
            int rb = data != null ? data.baseStats.range : 0;
            return Mathf.Max(1, wr + rb);
        }
    }
}