using UnityEngine;

namespace InGame
{
    public class FollowerUnit : MonoBehaviour
    {
        public Rigidbody2D rb;
        public Rigidbody2D commander;
        public SpriteRenderer sr;

        public float maxSpeed = 3.5f;
        public float slowRadius = 1.2f;
        public float stopRadius = 0.2f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        void FixedUpdate()
        {
            if (!commander) return;

            if (IsFrontOfCommander())
            {
                rb.linearVelocity = Vector2.zero;
            }
            else
            {
                var hits = Physics2D.RaycastAll(rb.position, commander.position - rb.position, 1f);
                var needSlow = false;
                foreach (var hit in hits)
                {
                    if (hit.transform.CompareTag("Minion"))
                    {
                        if (Vector2.Dot(commander.position - rb.position, (Vector2)hit.transform.position - rb.position) > 0)
                        {
                            needSlow = true;
                            break;
                        }
                    }
                }

                MoveTo(commander.position, needSlow);

                if (needSlow)
                    sr.color = Color.pink;
                else
                    sr.color = Color.gray;
            }
        }

        void MoveTo(Vector2 position, bool needSlow)
        {
            Vector2 toTarget = position - rb.position;
            float dist = toTarget.magnitude;

            if (dist < stopRadius)
            {
                rb.linearVelocity = Vector2.zero;
            }
            else
            {
                float speed = needSlow ? maxSpeed / 2f : maxSpeed;
                Vector2 desiredVel = toTarget.normalized * speed;
                rb.linearVelocity = desiredVel;
            }
        }

        bool IsFrontOfCommander()
        {
            var moveVec = commander.linearVelocity;
            var relative = rb.position - commander.position; // me - commander

            if (moveVec == Vector2.zero)
                return false;

            return Vector2.Dot(moveVec, relative) > 0;
        }
    }
}
