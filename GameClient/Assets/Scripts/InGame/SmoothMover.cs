using UnityEngine;

namespace InGame
{
    public class SmoothMover : MonoBehaviour
    {
        public float stopRadius = 0.2f;
        public float unitRadius = 0.4f;
        public float detectionDistance = 0.4f;
        public LayerMask structureMask;

        Rigidbody2D rb;
        Collider2D col;
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
            col = GetComponent<Collider2D>();
            animator = GetComponent<SpumAnimator>();
            structureMask = LayerMask.GetMask("Structure");
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

        public void EnableCollider(bool enable)
        {
            if (enable)
            {
                //rb.bodyType = RigidbodyType2D.Dynamic;
            }
            else
            {
                //rb.bodyType = RigidbodyType2D.Kinematic;
            }
            col.enabled = enable;
        }

        public bool IsDestination()
        {
            return (des - rb.position).magnitude < stopRadius || !hasDes;
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

                // 정면에 장애물 있으면 45도 각도로 회피
                var hit = Physics2D.CircleCast(transform.position, unitRadius, desiredVel, detectionDistance, structureMask);
                var finalDirection = desiredVel;
                if (hit.collider != null)
                {
                    var right45 = RotateVector(desiredVel, 45f);
                    if (!Physics2D.CircleCast(transform.position, unitRadius, right45, detectionDistance, structureMask))
                    {
                        finalDirection = right45;
                    }
                    else
                    {
                        var left45 = RotateVector(desiredVel, -45f);
                        finalDirection = left45;
                    }
                }

                //rb.linearVelocity = desiredVel;
                rb.linearVelocity = finalDirection;

                if (animator != null)
                    animator.SetState(SpumAnimator.State.Move);
            }
        }

        Vector2 RotateVector(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(rad);
            float cos = Mathf.Cos(rad);
            return new Vector2(cos * v.x - sin * v.y, sin * v.x + cos * v.y);
        }
    }
}
