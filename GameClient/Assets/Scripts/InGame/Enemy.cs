using GameModel;
using UnityEngine;

namespace InGame
{
    public class Enemy : MonoBehaviour
    {
        public Animator animator;

        UnitModel model;

        public void OnDamaged(long damage)
        {
            animator.SetTrigger("Damage");
        }
    }
}
