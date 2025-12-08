using GameModel;
using UnityEngine;

namespace InGame
{
    public class Enemy : MonoBehaviour
    {
        public Animator animator;

        UnitModel model;

        private void Start()
        {
            model = new UnitModel() { maxHp = 20 };
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
