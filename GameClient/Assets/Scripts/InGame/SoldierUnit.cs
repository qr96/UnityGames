using InGameModel;
using UnityEngine;

namespace InGame
{
    public class SoldierUnit : MonoBehaviour
    {
        public float detectRange;
        public float attackRange;
        public float moveSpeed = 5f;
        public float attackCool;
        public float destroyTime;

        public int TeamId;

        public SpriteRenderer sr;
        
        FollowLeader follow;
        SmoothMover mover;
        SpumAnimator animator;

        SoldierUnit attackTarget;

        State state;
        float attackEnd;
        bool isLeaderMoving;
        bool needRegroup; // trigger
        Vector2 formationPos;
        float destroyTimer;
        bool needCharge; // trigger
        Vector2 chargePos;

        UnitModel model;

        public enum State
        {
            Follow,
            Formation,
            Charge,
            Chase,
            Attack,
            Regroup,
            Dead
        }

        private void Awake()
        {
            follow = GetComponent<FollowLeader>();
            mover = GetComponent<SmoothMover>();
            animator = GetComponent<SpumAnimator>();

            model = new UnitModel() { maxHp = 10, attack = 2 };
            model.Spawn();
        }

        private void Update()
        {
            HandleTriggers();
            OnUpdateState(state);
        }

        public void SetLeader(Rigidbody2D rb)
        {
            follow.SetLeader(rb);
        }

        public void SetLeaderMoving(bool isMoving)
        {
            isLeaderMoving = isMoving;
        }

        public void SetNeedRegroup()
        {
            needRegroup = true;
        }

        public void MoveCommand(Vector2 position)
        {
            formationPos = position;
        }

        public void CommandCharge(Vector2 direction, float length)
        {
            needCharge = true;
            chargePos = mover.position + direction * length;
        }

        public void OnDamage(long damage)
        {
            model.OnDamage(damage);

            if (!model.IsAlive())
                SetState(State.Dead);
        }

        public bool IsAlive()
        {
            return model.IsAlive();
        }

        void SetState(State state)
        {
            OnEndState(this.state);
            this.state = state;
            OnStartState(state);
        }

        void HandleTriggers()
        {
            if (needRegroup)
            {
                if (state == State.Attack)
                {
                    if (!IsAttacking())
                    {
                        needRegroup = false;
                        SetState(State.Regroup);
                        return;
                    }
                }
                else if (state == State.Chase || state == State.Charge)
                {
                    needRegroup = false;
                    SetState(State.Regroup);
                    return;
                }
                else
                {
                    needRegroup = false;
                }
            }
        }

        void OnStartState(State state)
        {
            if (state == State.Follow)
            {
                follow.enabled = true;
            }
            else if (state == State.Formation)
            {
                mover.MoveTo(formationPos, moveSpeed);
            }
            else if (state == State.Charge)
            {
                mover.MoveTo(chargePos, moveSpeed);
            }
            else if (state == State.Attack)
            {
                mover.MoveStop();
                attackEnd = Time.time + attackCool;
                animator.SetState(SpumAnimator.State.Attack);
                Attack();
            }
            else if (state == State.Regroup)
            {
                follow.enabled = true;
            }
            else if (state == State.Dead)
            {
                mover.MoveStop();
                mover.EnableRigidbody(false);
                animator.SetState(SpumAnimator.State.Dead);
                destroyTimer = Time.time + destroyTime;
            }
        }

        void OnUpdateState(State state)
        {
            if (state == State.Follow)
            {
                if (needCharge)
                    SetState(State.Charge);
                else if (!isLeaderMoving)
                    SetState(State.Formation);
                else if (IsDetectEnemy(out attackTarget))
                    SetState(State.Chase);
            }
            else if (state == State.Formation)
            {
                // Controlled by commander
                if (needCharge)
                    SetState(State.Charge);
                else if (isLeaderMoving)
                    SetState(State.Follow);
                else if (IsDetectEnemy(out attackTarget))
                    SetState(State.Chase);
            }
            else if (state == State.Charge)
            {
                if (IsTargetInAttackRange())
                    SetState(State.Attack);
                else if (IsDetectEnemy(out attackTarget))
                    SetState(State.Chase);
                else if (mover.IsDestination())
                    SetState(State.Follow);
            }
            else if (state == State.Chase)
            {
                mover.MoveTo(attackTarget.transform.position, moveSpeed);

                if (IsTargetInAttackRange())
                    SetState(State.Attack);
                else if (!IsDetectEnemy(out attackTarget))
                    SetState(State.Follow);
            }
            else if (state == State.Attack)
            {
                if (!IsAttacking())
                {
                    if (IsTargetInAttackRange())
                        SetState(State.Attack);
                    else if (IsDetectEnemy(out attackTarget))
                        SetState(State.Chase);
                    else
                        SetState(State.Follow);
                }
            }
            else if (state == State.Regroup)
            {
                // Controlled by commander
                if (needCharge)
                {
                    SetState(State.Charge);
                }
                else
                {
                    if (follow.GetLeaderDis() < 2f)
                        SetState(State.Follow);
                }
            }
            else if (state == State.Dead)
            {
                if (Time.time > destroyTimer)
                {
                    gameObject.SetActive(false);
                }
            }

            needCharge = false;
        }

        void OnEndState(State state)
        {
            if (state == State.Follow)
            {
                follow.enabled = false;
            }
            else if (state == State.Regroup)
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
                    if (unit.TeamId != TeamId && unit.IsAlive())
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
                    if (unit.TeamId != TeamId && unit.IsAlive())
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        bool IsAttacking()
        {
            return Time.time < attackEnd;
        }

        void Attack()
        {
            var detects = Physics2D.OverlapCircleAll(transform.position, attackRange);
            foreach (var detect in detects)
            {
                var unit = detect.GetComponent<SoldierUnit>();
                if (unit != null)
                {
                    if (unit.TeamId != TeamId && unit.IsAlive())
                    {
                        unit.OnDamage(model.attack);
                        return;
                    }
                }
            }
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
