using System.Collections.Generic;
using UnityEngine;
using AutoBattler.Core;
using AutoBattler.Data;
using AutoBattler.Battle.FX;

namespace AutoBattler.Battle
{
    public static class SkillExecutor
    {
        public static void Execute(BattleUnit caster, SkillData skill,
                                   BattleUnit primaryTarget,
                                   List<BattleUnit> all, BattleField field)
        {
            // 시각 피드백 — 모든 스킬 공통
            ShowCasterFx(caster, skill);

            switch (skill.targetType)
            {
                case SkillTargetType.SingleEnemy:
                    if (primaryTarget != null && primaryTarget.IsAlive)
                    {
                        if (skill.projectilePrefab != null)
                        {
                            // 투사체 방식: 발사 후 도착 시 데미지
                            var tgt = primaryTarget;  // 클로저 캡처
                            ProjectileManager.Instance?.SpawnTowardUnit(
                                skill.projectilePrefab,
                                caster.transform.position,
                                tgt,
                                skill.projectileSpeed,
                                onArrive: () => {
                                    if (tgt != null && tgt.IsAlive)
                                        DamageOne(caster, tgt, skill);
                                });
                        }
                        else
                        {
                            // 즉발: 직선 표시 + 즉시 데미지
                            SkillVfxManager.Instance?.ShowSkillLine(
                                caster.transform.position, primaryTarget.transform.position,
                                isFriendly: false);
                            DamageOne(caster, primaryTarget, skill);
                        }
                    }
                    break;

                case SkillTargetType.AreaEnemy:
                    var center = primaryTarget != null ? primaryTarget.Cell : caster.Cell;
                    Vector3 centerWorld = primaryTarget != null
                        ? primaryTarget.transform.position
                        : caster.transform.position;

                    if (skill.projectilePrefab != null)
                    {
                        // 투사체 방식: 도착 지점 폭발 + AOE 적용
                        var capCenter = center;       // 클로저 캡처
                        var capCenterWorld = centerWorld;
                        ProjectileManager.Instance?.SpawnTowardPoint(
                            skill.projectilePrefab,
                            caster.transform.position,
                            centerWorld,
                            skill.projectileSpeed,
                            onArrive: () => {
                                SkillVfxManager.Instance?.ShowAoeCircle(capCenterWorld, skill.areaRadius);
                                ApplyAreaDamage(caster, capCenter, skill, all);
                            });
                    }
                    else
                    {
                        // 즉발
                        SkillVfxManager.Instance?.ShowAoeCircle(centerWorld, skill.areaRadius);
                        ApplyAreaDamage(caster, center, skill, all);
                    }
                    break;

                case SkillTargetType.Self:
                    if (skill.healAmount > 0f) caster.Heal(skill.healAmount);
                    break;

                case SkillTargetType.AllyLowestHP:
                    BattleUnit best = null;
                    float worst = float.MaxValue;
                    foreach (var u in all)
                    {
                        if (u == null || !u.IsAlive) continue;
                        if (u.Team != caster.Team) continue;
                        if (u.CurrentHP < worst) { worst = u.CurrentHP; best = u; }
                    }
                    if (best != null && skill.healAmount > 0f)
                    {
                        if (skill.projectilePrefab != null)
                        {
                            var capBest = best;
                            ProjectileManager.Instance?.SpawnTowardUnit(
                                skill.projectilePrefab,
                                caster.transform.position,
                                capBest,
                                skill.projectileSpeed,
                                onArrive: () => {
                                    if (capBest != null && capBest.IsAlive)
                                        capBest.Heal(skill.healAmount);
                                });
                        }
                        else
                        {
                            SkillVfxManager.Instance?.ShowSkillLine(
                                caster.transform.position, best.transform.position,
                                isFriendly: true);
                            best.Heal(skill.healAmount);
                        }
                    }
                    break;
            }
        }

        private static void ApplyAreaDamage(BattleUnit caster, Vector2Int center, SkillData skill,
                                            List<BattleUnit> all)
        {
            foreach (var u in all)
            {
                if (u == null || !u.IsAlive) continue;
                if (u.Team == caster.Team) continue;
                if (BattleGrid.ChebyshevDistance(u.Cell, center) <= skill.areaRadius)
                    DamageOne(caster, u, skill);
            }
        }

        // ─────────────────────────────────────────────────────────
        private static void ShowCasterFx(BattleUnit caster, SkillData skill)
        {
            // 시전자 위에 스킬 이름 텍스트 (FloatingText 재활용)
            if (FloatingTextManager.Instance != null && skill != null)
            {
                var color = IsHealing(skill)
                    ? new Color(0.4f, 1f, 0.5f)        // 힐 초록
                    : new Color(1f, 0.85f, 0.3f);     // 공격 노랑
                FloatingTextManager.Instance.Spawn(
                    caster.transform.position, skill.displayName, color, scale: 0.85f);
            }

            // 시전자 깜빡임 (UnitFlash 재활용)
            var flash = caster.GetComponentInChildren<UnitFlash>();
            if (flash != null)
            {
                if (IsHealing(skill)) flash.FlashHeal();
                else flash.FlashHit();
            }
        }

        private static bool IsHealing(SkillData skill) =>
            skill.targetType == SkillTargetType.Self
         || skill.targetType == SkillTargetType.AllyLowestHP;

        private static void DamageOne(BattleUnit caster, BattleUnit target, SkillData skill)
        {
            float power = caster.Stats.attack * Mathf.Max(0f, skill.damageMultiplier);
            BattleUnit.DealDamage(caster, target, power);
        }
    }
}