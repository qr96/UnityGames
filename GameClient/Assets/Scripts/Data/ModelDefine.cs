using UnityEngine;

namespace InGameModel
{
    public class UnitModel
    {
        public long maxHp;
        public long nowHp;

        public long attack;

        public void Spawn()
        {
            nowHp = maxHp;
        }

        public void OnDamage(long damage)
        {
            nowHp -= damage;
            if (nowHp < 0)
                nowHp = 0;
        }

        public bool IsAlive()
        {
            return nowHp > 0;
        }
    }
}
