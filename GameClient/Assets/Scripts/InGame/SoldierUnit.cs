using InGameModel;
using UnityEngine;

namespace InGame
{
    public class SoldierUnit : MonoBehaviour
    {
        public float detectRange;
        public float attackRange;
        public float moveSpeed = 5f;
        public float attackDuration; // 이게 끝나야 데미지 들어감
        public float attackDelay;
        public float destroyTime;
        
        public int TeamId;

        FollowLeader follow;
        SmoothMover mover;
        SpumAnimator animator;
        SpumSpriter spriter;

        SoldierUnit attackTarget;

        readonly float attackAngleCos = 0.707f;

        State state;
        float attackEnd;
        float attackDelayEnd;
        bool isLeaderMoving;
        Vector2 formationPos;
        float destroyTimer;
        bool isHoldMode;
        Vector2 attackDir;

        UnitModel model;

        public enum State
        {
            Follow,
            Formation,
            Chase,
            Attack,
            Combat,
            Dead
        }

        private void Awake()
        {
            follow = GetComponent<FollowLeader>();
            mover = GetComponent<SmoothMover>();
            animator = GetComponent<SpumAnimator>();
            spriter = GetComponent<SpumSpriter>();

            model = new UnitModel() { maxHp = 10, attack = 2 };
            model.Spawn();
        }

        private void Update()
        {
            OnUpdateState(state);
        }

        public void SetLeader(Rigidbody2D rb)
        {
            follow.SetLeader(rb);
        }

        public void SetColor(Color red)
        {
            spriter.SetColor(red);
        }

        public void SetHoldMode(bool holding)
        {
            isHoldMode = holding;
        }

        public void SetLeaderMoving(bool isMoving)
        {
            isLeaderMoving = isMoving;
        }

        public void MoveCommand(Vector2 position)
        {
            formationPos = position;
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
            else if (state == State.Attack)
            {
                mover.MoveStop();
                attackEnd = Time.time + attackDuration;
                attackDelayEnd = Time.time + attackDelay;
                animator.SetState(SpumAnimator.State.Attack);
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
                if (!isLeaderMoving)
                    SetState(State.Formation);
                else if (IsTargetInAttackRange())
                {
                    if (!isHoldMode || IsHoldPosition())
                    {
                        if (IsAttackDelayEnd())
                            SetState(State.Attack);
                    }
                }
                else if (IsDetectEnemy(out attackTarget))
                {
                    if (!isHoldMode)
                        SetState(State.Chase);
                }
            }
            else if (state == State.Formation)
            {
                if (isLeaderMoving)
                    SetState(State.Follow);
                else if (IsTargetInAttackRange())
                {
                    if (!isHoldMode || IsHoldPosition())
                    {
                        if (IsAttackDelayEnd())
                            SetState(State.Attack);
                    }
                }
                else if (IsDetectEnemy(out attackTarget))
                {
                    if (!isHoldMode)
                        SetState(State.Chase);
                }
            }
            else if (state == State.Chase)
            {
                mover.MoveTo(attackTarget.transform.position, moveSpeed);

                if (isHoldMode)
                    SetState(State.Follow);
                else if (IsTargetInAttackRange())
                {
                    if (IsAttackDelayEnd())
                        SetState(State.Attack);
                }
                else if (!IsDetectEnemy(out var attackTarget))
                    SetState(State.Follow);
            }
            else if (state == State.Attack)
            {
                // 공격 끝나고 데미지 들어감
                if (!IsAttacking())
                {
                    Attack();
                    SetState(State.Combat);
                }
                else if (isHoldMode && !IsHoldPosition())
                    SetState(State.Follow);
            }
            else if (state == State.Combat)
            {
                if (isHoldMode)
                    SetState(State.Follow);
                else if (IsTargetInAttackRange())
                {
                    if (IsAttackDelayEnd())
                        SetState(State.Attack);
                }
                else if (IsDetectEnemy(out attackTarget))
                    SetState(State.Chase);
                else
                    SetState(State.Follow);
            }
            else if (state == State.Dead)
            {
                if (Time.time > destroyTimer)
                {
                    gameObject.SetActive(false);
                }
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
                        attackDir = (unit.transform.position - transform.position).normalized;
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

        bool IsAttackDelayEnd()
        {
            return Time.time >= attackDelayEnd;
        }

        bool IsHoldPosition()
        {
            if (isLeaderMoving)
                return follow.GetLeaderDis() < 3f;
            else
                return mover.IsDestination();
        }

        void Attack()
        {
            var detects = Physics2D.OverlapCircleAll(transform.position, attackRange);
            foreach (var detect in detects)
            {
                var unit = detect.GetComponent<SoldierUnit>();
                if (unit != null)
                {
                    // 적이고 살아있음
                    if (unit.TeamId != TeamId && unit.IsAlive())
                    {
                        // 공격 각도 체크
                        if (CheckAttackDir(attackDir, unit.transform.position - transform.position, attackAngleCos))
                        {
                            unit.OnDamage(model.attack);
                            return;
                        }
                    }
                }
            }
        }

        // 공격 각도 범위 체크 코드. fanCos는 코사인 값. (시계, 반시계 45도씩이면 cos45 값 입력)
        bool CheckAttackDir(Vector2 attackDir, Vector2 targetDir, float fanCos)
        {
            var dot = Vector2.Dot(attackDir.normalized, targetDir.normalized);
            return dot > fanCos;
        }

        // 에디터에서 범위를 보기 위한 기즈모
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, detectRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)attackDir * attackRange);
        }
    }
}
