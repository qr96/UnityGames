using InGameModel;
using UnityEngine;

namespace InGame
{
    public class BaseUnit : MonoBehaviour
    {
        public int TeamId;

        protected UnitModel model;

        public void SetModel(UnitModel model)
        {
            this.model = model;
        }

        public void OnSpawn()
        {
            model.Spawn();
        }

        public void OnDamage(long damage)
        {
            model.OnDamage(damage);

            if (!model.IsAlive())
                OnDead();
        }

        public virtual void OnDead()
        {

        }

        public bool IsAlive()
        {
            return model.IsAlive();
        }
    }
}
