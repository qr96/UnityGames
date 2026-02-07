using System.Collections.Generic;

namespace InGame
{
    public abstract class BaseMagicScroll
    {
        // 스크롤 종류 반환
        public abstract Common.ScrollType GetScrollType();
        public abstract Common.ScrollTargetType GetTargetType();

        // 마법 로직 실행
        public void Execute(Common.ElementType[] slots, BaseUnit caster, List<BaseUnit> targets)
        {
            foreach (var target in targets)
            {
                OnExecute(slots, caster, target);
            }
        }

        public void Execute(Common.ElementType[] slots, BaseUnit caster, BaseUnit target)
        {
            OnExecute(slots, caster, target);
        }

        protected abstract void OnExecute(Common.ElementType[] slots, BaseUnit caster, BaseUnit target);
        // 특정 원소의 개수
        protected int GetElementCount(Common.ElementType[] slots, Common.ElementType elementType)
        {
            var count = 0;
            foreach (var slot in slots)
            {
                if (slot == elementType)
                    count++;
            }
            return count;
        }
    }

    public class BasicFireScroll : BaseMagicScroll
    {
        public override Common.ScrollType GetScrollType()
        {
            return Common.ScrollType.StandardFire;
        }

        public override Common.ScrollTargetType GetTargetType()
        {
            return Common.ScrollTargetType.Single;
        }

        protected override void OnExecute(Common.ElementType[] slots, BaseUnit caster, BaseUnit target)
        {
            var fireCount = GetElementCount(slots, Common.ElementType.Fire);
            var damage = caster.nowStat.attack * fireCount;

            if (target != null)
            {
                target.OnDamaged(damage);
            }
        }
    }

    public class BasicIceScroll : BaseMagicScroll
    {
        public override Common.ScrollType GetScrollType()
        {
            return Common.ScrollType.StandardIce;
        }

        public override Common.ScrollTargetType GetTargetType()
        {
            return Common.ScrollTargetType.Single;
        }

        protected override void OnExecute(Common.ElementType[] slots, BaseUnit caster, BaseUnit target)
        {
            var iceCount = GetElementCount(slots, Common.ElementType.Ice);
            var damage = caster.nowStat.attack * iceCount;

            if (target != null)
            {
                target.OnDamaged(damage);
            }
        }
    }
}
