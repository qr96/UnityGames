using System.Collections.Generic;
using UnityEngine;
using AutoBattler.Core;
using AutoBattler.Data;

namespace AutoBattler.Battle
{
    public static class SkillExecutor
    {
        public static void Execute(BattleUnit caster, SkillData skill,
                                   BattleUnit primaryTarget,
                                   List<BattleUnit> all, BattleField field)
        {
            switch (skill.targetType)
            {
                case SkillTargetType.SingleEnemy:
                    if (primaryTarget != null && primaryTarget.IsAlive)
                        DamageOne(caster, primaryTarget, skill);
                    break;

                case SkillTargetType.AreaEnemy:
                    var center = primaryTarget != null ? primaryTarget.Cell : caster.Cell;
                    foreach (var u in all)
                    {
                        if (u == null || !u.IsAlive) continue;
                        if (u.Team == caster.Team) continue;
                        if (BattleGrid.Distance(u.Cell, center) <= skill.areaRadius)
                            DamageOne(caster, u, skill);
                    }
                    break;

                case SkillTargetType.Self:
                    if (skill.healAmount > 0f) caster.Heal(skill.healAmount);
                    // 버프는 별도 시스템(BuffManager) 필요 — 여기서는 즉시 힐만 처리
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
                        best.Heal(skill.healAmount);
                    break;
            }
        }

        private static void DamageOne(BattleUnit caster, BattleUnit target, SkillData skill)
        {
            float power = caster.Stats.attack * Mathf.Max(0f, skill.damageMultiplier);
            BattleUnit.DealDamage(caster, target, power);
        }
    }
}
