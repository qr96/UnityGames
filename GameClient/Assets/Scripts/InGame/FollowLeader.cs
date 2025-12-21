using UnityEngine;

namespace InGame
{
    public class FollowLeader : MonoBehaviour
    {
        // Settings
        public float maxSpeed = 3.5f;
        public float frameDelay = 0.5f;

        SmoothMover mover;
        
        // Values
        Rigidbody2D leader;
        float nextFrameTime;

        private void Awake()
        {
            mover = GetComponent<SmoothMover>();
        }

        void Update()
        {
            if (!leader)
                return;
            if (Time.time < nextFrameTime)
                return;

            nextFrameTime = Time.time + frameDelay;

            if (IsFrontOfCommander())
            {
                mover.MoveStop();
            }
            else
            {
                // 앞에 아군이 있으면 속도 늦춤.
                //var toward = leader.position - mover.position;
                //var hits = Physics2D.RaycastAll(mover.position, toward, 1f);
                //var needSlow = false;
                //foreach (var hit in hits)
                //{
                //    if (hit.transform.CompareTag("Minion"))
                //    {
                //        if (Vector2.Dot(toward, (Vector2)hit.transform.position - mover.position) > 0)
                //        {
                //            needSlow = true;
                //            break;
                //        }
                //    }
                //}

                //mover.MoveTo(leader.position, needSlow ? maxSpeed / 2f : maxSpeed);
                mover.MoveTo(leader.position, maxSpeed);
            }
        }

        public void SetLeader(Rigidbody2D rb)
        {
            leader = rb;
        }

        public float GetLeaderDis()
        {
            return (leader.position - mover.position).magnitude;
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
