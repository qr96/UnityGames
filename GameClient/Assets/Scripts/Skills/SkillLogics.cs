using System.Collections.Generic;

namespace GameDefine
{
    public class SkillLogicManager
    {
        public static SkillLogicManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new SkillLogicManager();
                return instance;
            }
        }

        static SkillLogicManager instance;

        // 로직은 stateless → 캐싱해서 재사용
        readonly Dictionary<int, SkillEffectLogic> logicCache = new Dictionary<int, SkillEffectLogic>()
        {
            { 0, new AttackSkillLogic() },
            { 1, new AttackAndHealSkillLogic() },
            { 2, new AttackByHealthSkillLogic() },
            { 3, new AttackLowHealthSkillLogic() }
        };

        SkillEffectLogic fallback = new AttackSkillLogic();

        public SkillEffectLogic GetLogic(int skillId)
        {
            return logicCache.TryGetValue(skillId, out var logic) ? logic : fallback;
        }

        // 런타임에 스킬 추가 가능
        public void RegisterLogic(int skillId, SkillEffectLogic logic)
        {
            logicCache[skillId] = logic;
        }
    }

    public class AttackSkillLogic : SkillEffectLogic
    {
        readonly float[] attackMulti = { 1f, 1.5f, 2f };

        public override void Execute(BaseUnit caster, List<BaseUnit> enemies, BaseUnit target, int rank)
        {
            var damage = GetRankMultiResult(caster.GetDamage(), attackMulti, rank);
            target.OnDamaged(damage);
        }
    }

    public class AttackAndHealSkillLogic : SkillEffectLogic
    {
        readonly float[] attackMulti = { 0.8f, 0.9f, 1f };
        readonly float[] healMulti = { 0.1f, 0.15f, 0.2f };

        public override void Execute(BaseUnit caster, List<BaseUnit> enemies, BaseUnit target, int rank)
        {
            var damage = GetRankMultiResult(caster.GetDamage(), attackMulti, rank);

            var prevHp = target.nowStat.hp;
            target.OnDamaged(damage);

            // 실제로 가한 피해량의 N% 회복
            var realDamage = prevHp - target.nowStat.hp;
            var healAmount = GetRankMultiResult(realDamage, healMulti, rank);
            caster.Heal(healAmount);
        }
    }

    public class AttackByHealthSkillLogic : SkillEffectLogic
    {
        readonly float[] needHpMulti = { 0.05f, 0.05f, 0.05f };
        readonly float[] attackMulti = { 1.2f, 1.8f, 2.4f };

        public override void Execute(BaseUnit caster, List<BaseUnit> enemies, BaseUnit target, int rank)
        {
            var needHp = GetRankMultiResult(caster.nowStat.hp, needHpMulti, rank);
            var damage = GetRankMultiResult(caster.GetDamage(), attackMulti, rank);

            caster.OnDamaged(needHp);

            // 자해 후 시전자 사망 시 공격 취소
            if (caster.IsDead()) return;

            target.OnDamaged(damage);
        }
    }

    public class AttackLowHealthSkillLogic : SkillEffectLogic
    {
        readonly float needHpRate = 0.5f;
        readonly float[] attackMulti = { 1.5f, 2f, 2.5f };

        public override void Execute(BaseUnit caster, List<BaseUnit> enemies, BaseUnit target, int rank)
        {
            var damage = caster.GetDamage();

            if (target.nowStat.hp <= target.originStat.hp * needHpRate)
                damage = GetRankMultiResult(caster.GetDamage(), attackMulti, rank);

            target.OnDamaged(damage);
        }
    }
}

