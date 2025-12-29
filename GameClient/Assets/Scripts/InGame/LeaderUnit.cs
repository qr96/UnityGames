using InGameModel;
using System;
using UnityEngine;

namespace InGame
{
    public class LeaderUnit : BaseUnit
    {
        public IEventZone currentZone;
        public event Action<IEventZone> OnZoneChanged;

        Rigidbody2D rb;
        MinionFormationCommander formationCommander;
        SpumAnimator animator;

        bool alreadyStop;
        Vector2 input;
        Vector2 lastDir;

        State state;

        enum State
        {
            Idle,
            Attack,
            Dead
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            formationCommander = GetComponent<MinionFormationCommander>();
            animator = GetComponent<SpumAnimator>();

            SetModel(new UnitModel() { maxHp = 20, attack = 2 });
            OnSpawn();
        }

        void Update()
        {
            OnUpdateState(state);
        }

        public void SetInput(Vector2 dir)
        {
            input = dir.normalized;

            if (input != Vector2.zero)
                lastDir = input;
        }

        public bool IsStop()
        {
            return input == Vector2.zero;
        }

        public void SetHoldMode(bool holding)
        {
            formationCommander.SetHoldMode(holding);
        }

        public override void OnDead()
        {
            SetState(State.Dead);
        }

        public void SetEventZone(IEventZone eventZone)
        {
            currentZone = eventZone;
            OnZoneChanged?.Invoke(currentZone);
        }

        public void ExecuteZoneEvent(int index)
        {
            currentZone?.ExecuteEvent(index);
        }

        void SetState(State state)
        {
            this.state = state;
            OnStartState(state);
        }

        void OnStartState(State state)
        {
            if (state == State.Attack)
            {
                AttackTarget();
                animator.SetState(SpumAnimator.State.Attack);
                animator.SetDirection(attackDir);
            }
            else if (state == State.Dead)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.GetComponent<Collider2D>().enabled = false;
                rb.linearVelocity = Vector2.zero;
                animator.SetState(SpumAnimator.State.Dead);
            }
        }

        void OnUpdateState(State state)
        {
            if (state == State.Idle)
            {
                if (IsAttackDelayEnd() && IsTargetInAttackRange())
                    SetState(State.Attack);
                else
                    MoveLogic();
            }
            else if (state == State.Attack)
            {
                if (!IsAttacking())
                    SetState(State.Idle);
                else
                    MoveLogic();
            }
        }

        void MoveLogic()
        {
            rb.linearVelocity = input * moveSpeed;

            if (input != Vector2.zero)
            {
                if (alreadyStop)
                {
                    alreadyStop = false;
                    formationCommander.ReleaseFormation();
                    animator.SetState(SpumAnimator.State.Move);
                }
            }
            else
            {
                if (!alreadyStop)
                {
                    alreadyStop = true;
                    formationCommander.SetMinionsPosition(rb.position, lastDir);
                    animator.SetState(SpumAnimator.State.Idle);
                }
            }
        }
    }
}
