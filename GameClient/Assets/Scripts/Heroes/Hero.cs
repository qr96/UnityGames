using UnityEngine;
using AutoBattler.Core;
using AutoBattler.Data;

namespace AutoBattler.Heroes
{
    /// <summary>
    /// 전투 외부에서 들고 다니는 영웅 인스턴스.
    /// 영웅 정체성(이름/외형) = HeroData
    /// 직업(무기 외형/모션/수치) = currentJob
    /// 전투 진입 시 BattleUnit이 이 정보를 받아 초기화.
    /// </summary>
    [System.Serializable]
    public class Hero
    {
        public HeroData data;
        public JobData currentJob;

        // 액티브 슬롯 2개 — 인벤토리에서 교체 가능
        public SkillData skillA;
        public SkillData skillB;

        public Hero(HeroData baseData)
        {
            data = baseData;
            currentJob = baseData != null ? baseData.startingJob : null;
            skillA = baseData != null ? baseData.startingSkillA : null;
            skillB = baseData != null ? baseData.startingSkillB : null;
        }

        /// <summary>전직 — 직업 교체. 전직북 시스템이 들어오면 그쪽에서 호출.</summary>
        public void ChangeJob(JobData newJob)
        {
            if (newJob != null) currentJob = newJob;
        }

        /// <summary>최종 스탯 = 영웅 base + 직업 보너스.</summary>
        public Stats GetFinalStats()
        {
            var s = data != null ? data.baseStats : Stats.Zero;
            if (currentJob != null) s = s + currentJob.statBonus;
            return s;
        }

        /// <summary>기본공격 사거리 (1 + attackRange 보너스, 최소 1).</summary>
        public int GetBaseAttackRange()
        {
            int bonus = GetFinalStats().attackRange;
            return Mathf.Max(1, 1 + bonus);
        }

        /// <summary>기본공격 모션. UnitAnimator가 이를 보고 어떤 클립을 재생할지 결정.</summary>
        public AttackMotion GetAttackMotion()
        {
            return currentJob != null ? currentJob.attackMotion : AttackMotion.Swing;
        }
    }
}