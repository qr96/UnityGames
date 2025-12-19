using UnityEngine;

namespace InGame
{
    public class SoldierUnit : MonoBehaviour
    {
        public float detectRange;
        public float attackRange;
        public float moveSpeed = 5f;
        public float attackCool;

        public int TeamId;

        public SpriteRenderer sr;
        
        FollowLeader follow;
        SmoothMover mover;
        SpumAnimator animator;

        SoldierUnit attackTarget;

        State state;
        float attackEnd;

        enum State
        {
            Follow,
            Chase,
            Attack
        }

        private void Awake()
        {
            follow = GetComponent<FollowLeader>();
            mover = GetComponent<SmoothMover>();
            animator = GetComponent<SpumAnimator>();

            state = State.Follow;
        }

        private void Update()
        {
            if (state == State.Follow)
            {
                if (IsDetectEnemy(out attackTarget))
                {
                    state = State.Chase;

                    follow.enabled = false;

                    if (sr != null)
                        sr.color = Color.blue;
                }
            }
            else if (state == State.Chase)
            {
                mover.MoveTo(attackTarget.transform.position, moveSpeed);

                if (IsTargetInAttackRange())
                {
                    state = State.Attack;

                    mover.MoveStop();
                    attackEnd = Time.time + attackCool;
                    animator.SetState(SpumAnimator.State.Attack);

                    if (sr != null)
                        sr.color = Color.red;
                }
                else if (!IsDetectEnemy(out attackTarget))
                {
                    state = State.Follow;

                    follow.enabled = true;
                }
            }
            else if (state == State.Attack)
            {
                if (Time.time > attackEnd)
                {
                    if (IsTargetInAttackRange())
                    {
                        attackEnd = Time.time + attackCool;
                        animator.SetState(SpumAnimator.State.Attack);
                    }
                    else if (IsDetectEnemy(out attackTarget))
                    {
                        state = State.Chase;
                    }
                }
            }
        }

        public void SetLeader(Rigidbody2D rb)
        {
            follow.SetLeader(rb);
        }

        bool IsDetectEnemy(out SoldierUnit enemy)
        {
            var detects = Physics2D.OverlapCircleAll(transform.position, detectRange);
            foreach (var detect in detects)
            {
                var unit = detect.GetComponent<SoldierUnit>();
                if (unit != null)
                {
                    if (unit.TeamId != TeamId)
                    {
                        enemy = unit;
                        return true;
                    }
                }
            }

            enemy = null;
            return false;
        }

        bool IsTargetInAttackRange()
        {
            var detects = Physics2D.OverlapCircleAll(transform.position, attackRange);
            foreach (var detect in detects)
            {
                var unit = detect.GetComponent<SoldierUnit>();
                if (unit != null)
                {
                    if (unit.TeamId != TeamId)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // 에디터에서 범위를 보기 위한 기즈모
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, detectRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
