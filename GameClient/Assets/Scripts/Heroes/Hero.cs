using System.Collections.Generic;
using UnityEngine;
using AutoBattler.Core;
using AutoBattler.Data;

namespace AutoBattler.Heroes
{
    /// <summary>
    /// 전투 외부에서 들고 다니는 영웅 인스턴스.
    /// 전투에 진입할 때 BattleUnit을 만들어 이 정보를 주입한다.
    /// </summary>
    [System.Serializable]
    public class Hero
    {
        public HeroData data;
        public WeaponData weapon;
        public EquipmentData armor;
        public EquipmentData accessory;

        // 액티브 슬롯 2개 — 스킬 교체 불가 (런에서 합성/전직 외에는 변경 X)
        public SkillData skillA;
        public SkillData skillB;

        public Hero(HeroData baseData)
        {
            ApplyHeroData(baseData);
        }

        public void ApplyHeroData(HeroData newData)
        {
            data = newData;
            weapon = newData.startingWeapon;
            // 스킬은 보존이 원칙이지만 신규 시작 시에는 기본값으로
            if (skillA == null) skillA = newData.startingSkillA;
            if (skillB == null) skillB = newData.startingSkillB;
        }

        /// <summary>전직 (전직북 효과)</summary>
        public void Advance(HeroData branch)
        {
            if (branch == null) return;
            data = branch;
            // 무기/장비/스킬은 보존 — 디자인 의도에 따라 변경
        }

        /// <summary>3개 합성: 같은 스킬이 3개면 다음 등급으로</summary>
        public bool TryFuseSkill(SkillData target, int ownedCount)
        {
            if (target == null || target.upgradedVersion == null) return false;
            if (ownedCount < 3) return false;

            if (skillA == target) { skillA = target.upgradedVersion; return true; }
            if (skillB == target) { skillB = target.upgradedVersion; return true; }
            return false;
        }

        public Stats GetFinalStats()
        {
            var s = data.baseStats;
            if (weapon != null) s = s + weapon.statBonus;
            if (armor != null) s = s + armor.statBonus;
            if (accessory != null) s = s + accessory.statBonus;
            return s;
        }

        public int GetBaseAttackRange()
        {
            int r = weapon != null ? weapon.baseAttackRange : 1;
            return Mathf.Max(1, r + data.baseStats.range);
        }
    }
}
