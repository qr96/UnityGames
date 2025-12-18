using UnityEngine;

namespace InGame
{
    public class SmoothMover : MonoBehaviour
    {
        public Rigidbody2D rb;

        public float stopRadius = 0.2f;

        public Vector2 position
        {
            get { return rb.position; }
            set { rb.position = value; }
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
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
                rb.linearVelocity = Vector2.zero;
            }
            else
            {
                Vector2 desiredVel = toTarget.normalized * speed;
                rb.linearVelocity = desiredVel;
            }
        }

        public void MoveStop()
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
}
