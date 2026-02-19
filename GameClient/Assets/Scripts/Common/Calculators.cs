using GameDefine;


namespace Calculator
{
    public class Calculators
    {
        public static long GetDamage(BaseUnit unit)
        {
            return unit.nowStat.attack;
        }
    }
}
