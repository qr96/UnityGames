using UnityEditor;
using UnityEngine;

namespace InGame
{
    public class AILeader : MonoBehaviour
    {
        LeaderUnit unit;

        float stopRadius = 0.1f;
        float delay = 0.1f;
        float timer;
        Vector2 des;
        State state;
        float produceTimeEnd;

        BaseUnit attackTarget;
        CapturePoint targetPoint;

        enum State
        {
            Idle,
            Chase,
            MoveToPoint,
            MoveOutProduceZone,
            MoveToProduceZone,
            OnProducePoint,
            Dead
        }

        private void Awake()
        {
            unit = GetComponent<LeaderUnit>();

            unit.OnZoneChanged += OnZoneChanged;
        }

        private void OnDestroy()
        {
            unit.OnZoneChanged -= OnZoneChanged;
        }

        private void Update()
        {
            if (Time.time > timer)
            {
                timer = Time.time + delay;

                if (!unit.IsAlive())
                    return;

                OnUpdateState(state);
            }
        }

        void SetState(State state)
        {
            this.state = state;
            OnStartState(state);
        }

        void OnStartState(State state)
        {
            if (state == State.MoveToPoint)
            {
                targetPoint = FindNearCamp();
                des = targetPoint.transform.position + new Vector3(0f, -2f, 0f);
            }
            else if (state == State.MoveOutProduceZone)
            {
                des = targetPoint.produceUnitZone.transform.position + new Vector3(3f, 0f, 0f);
            }
            else if (state == State.MoveToProduceZone)
            {
                des = targetPoint.produceUnitZone.transform.position;
            }
            else if (state == State.OnProducePoint)
            {
                produceTimeEnd = Time.time + 3f;
                unit.SetInput(Vector2.zero);
            }    
        }

        void OnUpdateState(State state)
        {
            if (state == State.Idle)
            {
                if (unit.IsDetectEnemy(out attackTarget))
                    SetState(State.Chase);
                else
                    SetState(State.MoveToPoint);
            }
            else if (state == State.Chase)
            {
                if (attackTarget.IsAlive())
                    MoveTo(attackTarget.transform.position);
                else
                    SetState(State.Idle);
            }
            else if (state == State.MoveToPoint)
            {
                if (IsDestination(des))
                {
                    if (targetPoint.OwnTeamId == unit.TeamId)
                        SetState(State.MoveOutProduceZone);
                }
                else
                    MoveTo(des);
            }
            else if (state == State.MoveOutProduceZone)
            {
                if (IsDestination(des))
                    SetState(State.MoveToProduceZone);
                else
                    MoveTo(des);
            }
            else if (state == State.MoveToProduceZone)
            {
                if (IsDestination(des))
                    SetState(State.OnProducePoint);
                else
                    MoveTo(des);
            }
            else if (state == State.OnProducePoint)
            {
                if (Time.time > produceTimeEnd)
                    SetState(State.Idle);
            }
        }

        void OnZoneChanged(IEventZone eventZone)
        {
            if (eventZone is ProduceUnitZone)
            {
                if (FieldManager.Instance.TryGetProperty(unit.TeamId, out var prop))
                {
                    Debug.Log(prop.currentFood);
                    while (prop.currentFood > 20)
                        eventZone.ExecuteEvent(1);
                }
            }
        }

        CapturePoint FindNearCamp()
        {
            var list = FieldManager.Instance.capturePoints;
            if (list.Count == 0)
                return null;

            float nearDis = float.MaxValue;
            CapturePoint nearPoint = null;

            for (int i = 0; i < list.Count; i++)
            {
                var point = list[i];
                if (point.OwnTeamId == unit.TeamId)
                    continue;

                var dis = (point.transform.position - transform.position).sqrMagnitude;
                if (dis < nearDis)
                {
                    nearDis = dis;
                    nearPoint = point;
                }
            }

            return nearPoint;
        }

        void MoveTo(Vector2 destination)
        {
            var toTarget = destination - (Vector2)transform.position;

            if (IsDestination(destination))
                unit.SetInput(Vector2.zero);
            else
                unit.SetInput(toTarget);
        }

        bool IsDestination(Vector2 des)
        {
            var toTarget = des - (Vector2)transform.position;
            var distance = toTarget.magnitude;

            return distance < stopRadius;
        }
    }
}
