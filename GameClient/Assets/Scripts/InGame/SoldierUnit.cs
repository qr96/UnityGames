using InGameModel;
using UnityEngine;

namespace InGame
{
    public class SoldierUnit : BaseUnit
    {
        // Settings
        public float destroyTime;

        FollowLeader follow;
        SmoothMover mover;
        SpumAnimator animator;
        SpumSpriter spriter;

        MinionFormationCommander commander;
        BaseUnit attackTarget;

        State state;
        
        bool isLeaderMoving;
        Vector2 formationPos;
        float destroyTimer;
        bool isHoldMode;
        bool commandExecuted;

        public enum State
        {
            Idle,
            Chase,
            Attack,
            Dead,
            Destroyed
        }

        private void Awake()
        {
            follow = GetComponent<FollowLeader>();
            mover = GetComponent<SmoothMover>();
            animator = GetComponent<SpumAnimator>();
            spriter = GetComponent<SpumSpriter>();

            SetModel(new UnitModel() { maxHp = 10, attack = 2 });
            OnSpawn();
        }

        private void Update()
        {
            OnUpdateState(state);
        }

        public void SetLeader(Rigidbody2D rb)
        {
            follow.SetLeader(rb);
        }

        public void SetCommander(MinionFormationCommander commander)
        {
            this.commander = commander;
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
            commandExecuted = false;
        }

        public override void OnDead()
        {
            SetState(State.Dead);
        }

        void SetState(State state)
        {
            OnEndState(this.state);
            this.state = state;
            OnStartState(state);
        }

        void OnStartState(State state)
        {
            if (state == State.Idle)
            {
                if (isLeaderMoving)
                    follow.enabled = true;
                else
                    mover.MoveTo(formationPos, moveSpeed);
            }
            else if (state == State.Attack)
            {
                AttackTarget();
                mover.MoveStop();
                animator.SetState(SpumAnimator.State.Attack);
                animator.SetDirection(attackDir);
            }
            else if (state == State.Dead)
            {
                mover.MoveStop();
                mover.EnableCollider(false);
                animator.SetState(SpumAnimator.State.Dead);
                destroyTimer = Time.time + destroyTime;
            }
            else if (state == State.Destroyed)
            {
                if (commander != null)
                    commander.RemoveMinion(this);

                var poolable = GetComponent<Poolable>();
                if (poolable != null)
                    poolable.ReleaseSelf();
                else
                {
                    Debug.LogError("Failed to ReleaseSelf");
                    Destroy(gameObject);
                }
            }
        }

        void OnUpdateState(State state)
        {
            if (state == State.Idle)
            {
                if (isHoldMode)
                {
                    if (isLeaderMoving)
                    {
                        if (!follow.enabled)
                            SetState(State.Idle);
                        else if (follow.GetLeaderDis() > 3f)
                        {
                            // must go to leader.
                        }
                        else if (IsTargetInAttackRange())
                        {
                            if (IsAttackDelayEnd())
                                SetState(State.Attack);
                        }
                    }
                    else
                    {
                        if (follow.enabled)
                            SetState(State.Idle);
                        else if ((formationPos - mover.position).magnitude > 0.2f)
                        {
                            // must go to formation position.
                            if (!commandExecuted)
                            {
                                SetState(State.Idle);
                                commandExecuted = true;
                            }
                        }
                        else if (IsTargetInAttackRange())
                        {
                            if (IsAttackDelayEnd())
                                SetState(State.Attack);
                        }
                    }
                }
                else
                {
                    if ((isLeaderMoving && !follow.enabled) || (!isLeaderMoving && follow.enabled))
                        SetState(State.Idle);
                    else if (IsTargetInAttackRange())
                    {
                        if (IsAttackDelayEnd())
                            SetState(State.Attack);
                    }
                    else if (IsDetectEnemy(out attackTarget))
                    {
                        SetState(State.Chase);
                    }
                }
            }
            else if (state == State.Chase)
            {
                mover.MoveTo(attackTarget.transform.position, moveSpeed);

                if (isHoldMode)
                    SetState(State.Idle);
                else if (IsTargetInAttackRange())
                {
                    if (IsAttackDelayEnd())
                        SetState(State.Attack);
                }
                else if (!IsDetectEnemy(out var attackTarget))
                    SetState(State.Idle);
            }
            else if (state == State.Attack)
            {
                if (!IsAttacking())
                    SetState(State.Idle);
                else if (isHoldMode && !IsHoldPosition())
                {
                    CancelAttack();
                    SetState(State.Idle);
                }
            }
            else if (state == State.Dead)
            {
                if (Time.time > destroyTimer)
                    SetState(State.Destroyed);
            }
        }

        void OnEndState(State state)
        {
            if (state == State.Idle)
            {
                follow.enabled = false;
            }
        }

        bool IsHoldPosition()
        {
            if (isLeaderMoving)
                return follow.GetLeaderDis() < 3f;
            else
                return mover.IsDestination();
        }
    }
}
