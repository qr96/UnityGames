using UnityEngine;

namespace InGame
{
    public class Enemy : MonoBehaviour
    {
        public Animator animator;

        public void OnDamaged()
        {
            animator.SetTrigger("Damage");
        }
    }
}
