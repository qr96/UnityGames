using UnityEngine;

namespace GameModel
{
    public class UnitModel
    {
        public long maxHp { get; set; }
        public long nowHp { get; private set; }

        public void Spawn()
        {
            nowHp = maxHp;
        }

        public void SetHp(long hp)
        {
            nowHp = hp;
            if (hp < 0)
                nowHp = 0;
            if (hp > maxHp)
                nowHp = maxHp;
        }

        public bool IsDead()
        {
            return nowHp <= 0;
        }
    }
}
