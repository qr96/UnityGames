using System;
using UnityEngine;
using AutoBattler.Core;
using AutoBattler.Data;

namespace AutoBattler.Rounds
{
    /// <summary>
    /// 라운드 종료 시 제시되는 단일 보상 후보.
    /// 종류는 무기/장비/스킬 셋 중 하나.
    /// </summary>
    [Serializable]
    public class RewardOption
    {
        public RewardType type;
        public WeaponData weapon;
        public EquipmentData equipment;
        public SkillData skill;

        public string DisplayName => type switch
        {
            RewardType.Weapon    => weapon != null    ? weapon.displayName    : "(빈 무기)",
            RewardType.Equipment => equipment != null ? equipment.displayName : "(빈 장비)",
            RewardType.Skill     => skill != null     ? skill.displayName     : "(빈 스킬)",
            _ => "?"
        };
    }
}
