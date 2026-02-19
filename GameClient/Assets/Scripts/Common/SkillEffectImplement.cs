using System.Collections.Generic;
using UnityEngine;

namespace GameDefine
{
    public abstract class SkillEffectLogic : ScriptableObject
    {
        public abstract void Execute(BaseUnit caster, List<BaseUnit> enemies, BaseUnit target, int rank);

        public float GetRankMultiplier(int rank, float[] rankMultipliers)
        {
            if (rank < 1 || rank > rankMultipliers.Length)
                return 0f;

            return rankMultipliers[rank - 1];
        }

        public long GetRankMultiResult(long factor, float[] rankMultipliers, int rank)
        {
            var multiplier = GetRankMultiplier(rank, rankMultipliers);

            return (long)(factor * multiplier);
        }
    }
}
