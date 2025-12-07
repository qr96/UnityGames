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
            if (model.IsDead())
                gameObject.SetActive(false);

            animator.SetTrigger("Damage");
        }
    }
}
