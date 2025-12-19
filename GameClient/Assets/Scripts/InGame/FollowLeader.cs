using UnityEngine;

namespace InGame
{
    public class FollowLeader : MonoBehaviour
    {
        // Settings
        SmoothMover mover;

        // Values
        Rigidbody2D leader;

        public SpriteRenderer sr;

        public float maxSpeed = 3.5f;

        private void Awake()
        {
            mover = GetComponent<SmoothMover>();
        }

        void FixedUpdate()
        {
            if (!leader) return;

            if (IsFrontOfCommander())
            {
                mover.MoveStop();
            }
            else
            {
                // 앞에 아군이 있으면 속도 늦춤.
                var toward = leader.position - mover.position;
                var hits = Physics2D.RaycastAll(mover.position, toward, 1f);
                var needSlow = false;
                foreach (var hit in hits)
                {
                    if (hit.transform.CompareTag("Minion"))
                    {
                        if (Vector2.Dot(toward, (Vector2)hit.transform.position - mover.position) > 0)
                        {
                            needSlow = true;
                            break;
                        }
                    }
                }

                mover.MoveTo(leader.position, needSlow ? maxSpeed / 2f : maxSpeed);

                //if (needSlow)
                //    sr.color = Color.pink;
                //else
                //    sr.color = Color.gray;
            }
        }

        private void OnDisable()
        {
            mover.MoveStop();
        }

        public void SetLeader(Rigidbody2D rb)
        {
            leader = rb;
        }

        bool IsFrontOfCommander()
        {
            var moveVec = leader.linearVelocity;
            var relative = mover.position - leader.position; // me - commander

            if (moveVec == Vector2.zero)
                return false;

            return Vector2.Dot(moveVec, relative) > 0;
        }
    }
}
