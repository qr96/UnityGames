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

        bool isMovePrevFrame; // 이전 프레임에서 움직임 여부
        Vector2 input;
        Vector2 lastDir;
        float respawnTime = 5f;
        float respawnTimeEnd;

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

        public void Spawn()
        {
            OnSpawn();
            rb.GetComponent<Collider2D>().enabled = true;
            animator.SetState(SpumAnimator.State.Respawn);
            isMovePrevFrame = false;
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
                rb.GetComponent<Collider2D>().enabled = false;
                rb.linearVelocity = Vector2.zero;
                animator.SetState(SpumAnimator.State.Dead);
                formationCommander.SetMinionsPosition(rb.position, lastDir);
                respawnTimeEnd = Time.time + respawnTime;
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
            else if (state == State.Dead)
            {
                if (Time.time > respawnTimeEnd)
                {
                    if (RespawnLogic())
                        SetState(State.Idle);
                }
            }
        }

        void MoveLogic()
        {
            rb.linearVelocity = input * moveSpeed;

            if (input != Vector2.zero)
            {
                // 이동 시 한 번만 호출
                if (!isMovePrevFrame)
                {
                    formationCommander.ReleaseFormation();
                    animator.SetState(SpumAnimator.State.Move);
                }

                isMovePrevFrame = true;
            }
            else
            {
                // 정지 시 한 번만 호출
                if (isMovePrevFrame)
                {
                    formationCommander.SetMinionsPosition(rb.position, lastDir);
                    animator.SetState(SpumAnimator.State.Idle);
                }

                isMovePrevFrame = false;
            }
        }

        bool RespawnLogic()
        {
            var capturePoints = FieldManager.Instance.capturePoints;
            CapturePoint nearPoint = null;
            float nearDis = float.MaxValue;

            foreach (var point in capturePoints)
            {
                if (point.OwnTeamId == TeamId)
                {
                    var dis = (point.transform.position - transform.position).magnitude;
                    if (dis < nearDis)
                    {
                        nearDis = dis;
                        nearPoint = point;
                    }
                }
            }

            if (nearPoint != null)
            {
                rb.position = nearPoint.transform.position + new Vector3(0f, -2f, 0f);
                Spawn();
                return true;
            }

            return false;
        }
    }
}
