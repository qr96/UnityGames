using System.Collections.Generic;
using UnityEngine;
using AutoBattler.Core;
using AutoBattler.Data;
using AutoBattler.Battle.FX;

namespace AutoBattler.Battle
{
    /// <summary>
    /// 스킬 실행 — 3축 타깃팅(Team/Range/Selector) 기반.
    ///
    /// 흐름:
    ///   1) 후보 수집 (TeamFilter + RangeFilter)
    ///   2) 후보 중 선택 (Selector)
    ///   3) 효과 적용 (damage / heal)
    ///
    /// 발동 가능 여부 사전 검사는 BattleUnit.CanCastSkill 에서 수행.
    /// 여기 도달했으면 발동은 보장됨 (단 타깃이 그 사이 죽을 수는 있음).
    /// </summary>
    public static class SkillExecutor
    {
        public static void Execute(BattleUnit caster, SkillData skill,
                                   List<BattleUnit> all, BattleField field)
        {
            // 시각 피드백 — 모든 스킬 공통
            ShowCasterFx(caster, skill);

            // Self 는 후보 탐색 생략
            if (skill.teamFilter == SkillTeamFilter.Self)
            {
                ApplyEffect(caster, caster, skill, all);
                return;
            }

            var target = ResolveTarget(caster, skill, all);
            if (target == null || !target.IsAlive) return;

            // 투사체가 있으면 도착 시 효과
            if (skill.projectilePrefab != null)
            {
                var capTarget = target;
                ProjectileManager.Instance?.SpawnTowardUnit(
                    skill.projectilePrefab,
                    caster.transform.position,
                    capTarget,
                    skill.projectileSpeed,
                    onArrive: () =>
                    {
                        if (capTarget != null && capTarget.IsAlive)
                            ApplyEffect(caster, capTarget, skill, all);
                    });
            }
            else
            {
                bool friendly = skill.teamFilter == SkillTeamFilter.Ally;
                SkillVfxManager.Instance?.ShowSkillLine(
                    caster.transform.position, target.transform.position, friendly);
                ApplyEffect(caster, target, skill, all);
            }
        }

        // ─────────────────────────────────────────────────────────
        // 후보 수집 + 선택
        // ─────────────────────────────────────────────────────────

        /// <summary>
        /// TeamFilter + RangeFilter로 후보를 모으고, Selector로 한 명 고른다.
        /// 회복 스킬은 만피 대상을 후보에서 제외 (낭비 방지).
        /// </summary>
        public static BattleUnit ResolveTarget(BattleUnit caster, SkillData skill,
                                               List<BattleUnit> all)
        {
            bool isHeal = skill.healAmount > 0f;

            BattleUnit best = null;
            float bestScore = 0f;

            foreach (var u in all)
            {
                if (u == null || !u.IsAlive) continue;
                if (!MatchesTeam(caster, u, skill.teamFilter)) continue;
                if (!MatchesRange(caster, u, skill)) continue;
                if (isHeal && u.CurrentHP >= u.Stats.maxHp) continue; // 만피 제외

                float score = ScoreCandidate(caster, u, skill.selector);
                if (best == null || score < bestScore)
                {
                    best = u;
                    bestScore = score;
                }
            }
            return best;
        }

        private static bool MatchesTeam(BattleUnit caster, BattleUnit candidate, SkillTeamFilter filter)
        {
            switch (filter)
            {
                case SkillTeamFilter.Enemy: return candidate.Team != caster.Team;
                case SkillTeamFilter.Ally: return candidate.Team == caster.Team;
                case SkillTeamFilter.Self: return candidate == caster;
            }
            return false;
        }

        private static bool MatchesRange(BattleUnit caster, BattleUnit candidate, SkillData skill)
        {
            if (skill.rangeFilter == SkillRangeFilter.Global) return true;
            return BattleGrid.InAttackRange(caster.Cell, candidate.Cell, skill.range);
        }

        /// <summary>점수가 낮을수록 우선. (ResolveTarget이 최소값 선택)</summary>
        private static float ScoreCandidate(BattleUnit caster, BattleUnit candidate,
                                            SkillTargetSelector selector)
        {
            switch (selector)
            {
                case SkillTargetSelector.Nearest:
                    return BattleGrid.Distance(caster.Cell, candidate.Cell);
                case SkillTargetSelector.LowestHP:
                    return candidate.CurrentHP;
                case SkillTargetSelector.Self:
                    return 0f; // Self는 caster 자신만 후보에 들어옴
            }
            return 0f;
        }

        // ─────────────────────────────────────────────────────────
        // 효과 적용
        // ─────────────────────────────────────────────────────────

        private static void ApplyEffect(BattleUnit caster, BattleUnit target, SkillData skill,
                                        List<BattleUnit> all)
        {
            if (skill.splashRadius > 0)
            {
                ApplySplash(caster, target, skill, all);
                return;
            }

            ApplySingleEffect(caster, target, skill);
        }

        /// <summary>광역 효과 — 타깃 + 같은 진영 주변 N칸 (체비셰프) 에 적용.</summary>
        private static void ApplySplash(BattleUnit caster, BattleUnit target, SkillData skill,
                                        List<BattleUnit> all)
        {
            // 시각 표시 — 타깃 중심 AOE 원
            SkillVfxManager.Instance?.ShowAoeCircle(target.transform.position, skill.splashRadius);

            // 광역 대상 진영 = 스킬의 TeamFilter 기준
            //   - Enemy 스킬: 시전자의 적 진영 (= caster.Team과 다른 팀)
            //   - Ally/Self 스킬: 시전자와 같은 진영
            bool affectsEnemyTeam = skill.teamFilter == SkillTeamFilter.Enemy;

            foreach (var u in all)
            {
                if (u == null || !u.IsAlive) continue;
                bool isEnemyOfCaster = u.Team != caster.Team;
                if (affectsEnemyTeam != isEnemyOfCaster) continue;
                if (BattleGrid.ChebyshevDistance(u.Cell, target.Cell) > skill.splashRadius) continue;

                ApplySingleEffect(caster, u, skill);
            }
        }

        private static void ApplySingleEffect(BattleUnit caster, BattleUnit target, SkillData skill)
        {
            if (skill.healAmount > 0f)
            {
                target.Heal(skill.healAmount);
            }
            if (skill.damageMultiplier > 0f && target.Team != caster.Team)
            {
                float power = caster.Stats.attack * skill.damageMultiplier;
                BattleUnit.DealDamage(caster, target, power);
            }
        }

        // ─────────────────────────────────────────────────────────
        // 연출
        // ─────────────────────────────────────────────────────────

        private static void ShowCasterFx(BattleUnit caster, SkillData skill)
        {
            if (FloatingTextManager.Instance != null && skill != null)
            {
                var color = IsFriendlyEffect(skill)
                    ? new Color(0.4f, 1f, 0.5f)
                    : new Color(1f, 0.85f, 0.3f);
                FloatingTextManager.Instance.Spawn(
                    caster.transform.position, skill.displayName, color, scale: 0.85f);
            }

            var flash = caster.GetComponentInChildren<UnitFlash>();
            if (flash != null)
            {
                if (IsFriendlyEffect(skill)) flash.FlashHeal();
                else flash.FlashHit();
            }
        }

        private static bool IsFriendlyEffect(SkillData skill) =>
            skill.teamFilter == SkillTeamFilter.Self
         || skill.teamFilter == SkillTeamFilter.Ally;
    }
}