using UnityEngine;

namespace GameModel
{
    public class UnitModel
    {
        public long hp { get; set; }

        public bool IsDead()
        {
            return hp <= 0;
        }
    }
}

