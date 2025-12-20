using UnityEngine;

namespace InGame
{
    public class SmoothMover : MonoBehaviour
    {
        public float stopRadius = 0.2f;

        Rigidbody2D rb;
        SpumAnimator animator;

        Vector2 des;
        float speed;
        bool hasDes;
        float moveTimeout;

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

        private void Update()
        {
            MoveRb();
        }

        public void MoveTo(Vector2 position, float speed)
        {
            des = position;
            this.speed = speed;
            hasDes = true;
            moveTimeout = Time.time + (position - rb.position).magnitude / speed;
        }

        public void MoveStop()
        {
            rb.linearVelocity = Vector2.zero;
            hasDes = false;
            moveTimeout = 0f;

            if (animator != null)
                animator.SetState(SpumAnimator.State.Idle);
        }

        void MoveRb()
        {
            if (!hasDes)
                return;
            else if (Time.time > moveTimeout)
            {
                MoveStop();
                return;
            }

            var toTarget = des - rb.position;
            var distance = toTarget.magnitude;

            if (distance < stopRadius)
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
    }
}
