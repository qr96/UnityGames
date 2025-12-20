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

        public enum State
        {
            Follow,
            Formation,
            Chase,
            Attack
        }

        private void Awake()
        {
            follow = GetComponent<FollowLeader>();
            mover = GetComponent<SmoothMover>();
            animator = GetComponent<SpumAnimator>();
        }

        private void Update()
        {
            if (state == State.Follow)
            {
                if (IsDetectEnemy(out attackTarget))
                {
                    SetState(State.Chase);
                }
            }
            else if (state == State.Formation)
            {
                // Controlled by commander
            }
            else if (state == State.Chase)
            {
                mover.MoveTo(attackTarget.transform.position, moveSpeed);

                if (IsTargetInAttackRange())
                {
                    SetState(State.Attack);
                }
                else if (!IsDetectEnemy(out attackTarget))
                {
                    SetState(State.Follow);
                }
            }
            else if (state == State.Attack)
            {
                if (Time.time > attackEnd)
                {
                    if (IsTargetInAttackRange())
                    {
                        SetState(State.Attack);
                    }
                    else if (IsDetectEnemy(out attackTarget))
                    {
                        SetState(State.Chase);
                    }
                }
            }
        }

        public void SetLeader(Rigidbody2D rb)
        {
            follow.SetLeader(rb);
        }

        public void SetState(State state)
        {
            OnEndState(this.state);
            this.state = state;
            OnStartState(state);
        }

        public void MoveCommand(Vector2 position)
        {
            if (state == State.Formation)
            {
                mover.MoveTo(position, moveSpeed);
            }
        }

        void OnStartState(State state)
        {
            if (state == State.Follow)
            {
                follow.enabled = true;
            }
            else if (state == State.Attack)
            {
                mover.MoveStop();
                attackEnd = Time.time + attackCool;
                animator.SetState(SpumAnimator.State.Attack);
            }
        }

        void OnEndState(State state)
        {
            if (state == State.Follow)
            {
                follow.enabled = false;
            }
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
