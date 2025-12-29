using UnityEngine;

namespace InGame
{
    public class AILeader : MonoBehaviour
    {
        LeaderUnit unit;

        float delay = 0.2f;
        float timer;
        bool wantToProduce;

        BaseUnit attackTarget;
        CapturePoint targetPoint;

        private void Awake()
        {
            unit = GetComponent<LeaderUnit>();
        }

        private void Update()
        {
            if (Time.time > timer)
            {
                timer = Time.time + delay;

                if (!unit.IsAlive())
                    return;

                if (attackTarget == null)
                {
                    if (!unit.IsDetectEnemy(out attackTarget))
                    {
                        if (targetPoint == null)
                        {
                            targetPoint = FindNearCamp();
                        }
                        else
                        {
                            if (targetPoint.OwnTeamId == unit.TeamId)
                            {
                                targetPoint = null;
                            }
                            else
                            {
                                var dir = targetPoint.transform.position - transform.position + new Vector3(0f, -2f, 0f);
                                unit.SetInput(dir);
                            }
                        }
                    }
                }
                else
                {
                    if (!attackTarget.IsAlive())
                    {
                        attackTarget = null;
                    }
                    else
                    {
                        var dir = attackTarget.transform.position - transform.position;
                        unit.SetInput(dir);
                    }
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
    }
}
