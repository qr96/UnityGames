using GameModel;
using UnityEngine;

namespace InGame
{
    public class Enemy : MonoBehaviour
    {
        public Animator animator;

        UnitModel model;

        public void Spawn(UnitModel model)
        {
            this.model = model;
            model.Spawn();
        }

        public void OnDamaged(long damage)
        {
            model.SetHp(model.nowHp - damage);
            animator.SetTrigger("Damage");

            Managers.UI.hudLayout.RegisterHpGuage(transform);
            Managers.UI.hudLayout.UpdateHpGuageValue(transform, model.maxHp, model.nowHp);

            if (model.IsDead())
                OnDead();
        }

        void OnDead()
        {
            gameObject.SetActive(false);
            Managers.UI.hudLayout.RemoveHpGuage(transform);
        }
    }
}
