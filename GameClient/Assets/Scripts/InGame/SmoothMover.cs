using UnityEngine;

namespace InGame
{
    public class SmoothMover : MonoBehaviour
    {
        Rigidbody2D rb;
        SpumAnimator animator;

        public float stopRadius = 0.2f;

        public Vector2 position
        {
            get { return rb.position; }
            set { rb.position = value; }
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<SpumAnimator>();
        }

        private void OnDisable()
        {
            MoveStop();
        }

        public void MoveTo(Vector2 position, float speed)
        {
            Vector2 toTarget = position - rb.position;
            float dist = toTarget.magnitude;

            if (dist < stopRadius)
            {
                MoveStop();
            }
            else
            {
                Vector2 desiredVel = toTarget.normalized * speed;
                rb.linearVelocity = desiredVel;

                if (animator != null)
                    animator.SetState(SpumAnimator.State.Move);
            }
        }

        public void MoveStop()
        {
            rb.linearVelocity = Vector2.zero;

            if (animator != null)
                animator.SetState(SpumAnimator.State.Idle);
        }
    }
}
