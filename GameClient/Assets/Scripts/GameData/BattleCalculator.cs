using UnityEngine;

public class BattleCalculator
{
    public static long GetDamage(long atk, long def)
    {
        var damage = atk - def;
        if (damage < 0) damage = 0;
        return damage;
    }
}
