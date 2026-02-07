using System.Collections.Generic;

namespace InGame
{
    public abstract class BaseMagicScroll
    {
        // 스크롤 사용 가능 여부 (이미 사용된 경우 false. 기본 스크롤은 무제한 사용으로 계속 true)
        public abstract bool IsUsable();

        // 스크롤 종류 반환
        public abstract Common.ScrollType GetScrollType();

        // 마법 로직 실행
        public abstract void Execute(Common.ElementType[] slots, BaseUnit caster, List<BaseUnit> targets);

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

        public override bool IsUsable()
        {
            return true;
        }

        public override void Execute(Common.ElementType[] slots, BaseUnit caster, List<BaseUnit> targets)
        {
            var fireCount = GetElementCount(slots, Common.ElementType.Fire);
            var damage = caster.nowStat.attack * fireCount;

            if (targets != null && targets.Count > 0)
            {
                targets[0].OnDamaged(damage);
            }
        }
    }

    public class BasicIceScroll : BaseMagicScroll
    {
        public override Common.ScrollType GetScrollType()
        {
            return Common.ScrollType.StandardIce;
        }

        public override bool IsUsable()
        {
            return true;
        }

        public override void Execute(Common.ElementType[] slots, BaseUnit caster, List<BaseUnit> targets)
        {
            var iceCount = GetElementCount(slots, Common.ElementType.Ice);
            var damage = caster.nowStat.attack * iceCount;

            if (targets != null && targets.Count > 0)
            {
                targets[0].OnDamaged(damage);
            }
        }
    }
}
