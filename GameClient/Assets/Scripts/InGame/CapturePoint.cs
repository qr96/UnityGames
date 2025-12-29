using InGameModel;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace InGame
{
    public class CapturePoint : MonoBehaviour
    {
        // Test
        public SpriteRenderer flag;

        public int PointId;
        public int OwnTeamId;
        public int foodProduction;
        public float occupySpeed;

        public Action<CapturePoint, int, int> OnChangeOwner;

        Dictionary<int, int> unitCount = new Dictionary<int, int>(); //<teamId, count>
        float occupyProggress;
        int progressTeamCount; // 점령 진행중인 팀 수
        int progressTeamId;

        private void Update()
        {
            if (progressTeamCount == 1)
            {
                var nowTeamId = FindProgressTeamId();

                // 중립상태
                if (OwnTeamId == 0)
                {
                    // 처음 점령 시도
                    if (progressTeamId == 0)
                    {
                        progressTeamId = nowTeamId;
                        occupyProggress = Mathf.Min(1f, occupyProggress + occupySpeed * Time.deltaTime);
                    }
                    else
                    {
                        if (nowTeamId == progressTeamId)
                        {
                            occupyProggress = Mathf.Min(1f, occupyProggress + occupySpeed * Time.deltaTime);

                            if (occupyProggress == 1f)
                                ChangeOwner(nowTeamId);
                        }
                        else // 새로운 팀 점령 시도
                        {
                            occupyProggress = Mathf.Max(0f, occupyProggress - occupySpeed * Time.deltaTime);

                            if (occupyProggress == 0f)
                                progressTeamId = 0;
                        }
                    }

                    OnProgress(progressTeamId, occupyProggress);
                }
                else
                {
                    // 점령지 회복
                    if (nowTeamId == OwnTeamId)
                    {
                        occupyProggress = Mathf.Min(1f, occupyProggress + occupySpeed * Time.deltaTime);
                    }
                    else
                    {
                        occupyProggress = Mathf.Max(0f, occupyProggress - occupySpeed * Time.deltaTime);

                        if (occupyProggress == 0f)
                            ChangeOwner(0);
                    }

                    OnProgress(OwnTeamId, occupyProggress);
                }
            }
        }

        public void ChangeOwner(int ownTeamId)
        {
            if (ownTeamId == OwnTeamId)
                return;

            OnChangeOwner?.Invoke(this, OwnTeamId, ownTeamId);
            OwnTeamId = ownTeamId;
        }

        void OnTriggerEnter2D(Collider2D collision)
        {
            var unit = collision.GetComponent<BaseUnit>();
            if (unit != null)
            {
                if (unitCount.ContainsKey(unit.TeamId))
                    unitCount[unit.TeamId]++;
                else
                    unitCount.Add(unit.TeamId, 1);

                UpdateTeamCount();
            }
        }

        void OnTriggerExit2D(Collider2D collision)
        {
            var unit = collision.GetComponent<BaseUnit>();
            if (unit != null)
            {
                if (unitCount.ContainsKey(unit.TeamId))
                    unitCount[unit.TeamId] = Mathf.Max(0, unitCount[unit.TeamId] - 1);

                UpdateTeamCount();
            }
        }

        void UpdateTeamCount()
        {
            progressTeamCount = 0;

            foreach (var count in unitCount.Values)
            {
                if (count > 0)
                    progressTeamCount++;
            }
        }

        int FindProgressTeamId()
        {
            foreach (var auto in unitCount)
            {
                if (auto.Value > 0)
                    return auto.Key;
            }

            return 0;
        }

        Color[] flagColors = new Color[6] { Color.gray, Color.blue, Color.red, Color.yellow, Color.green, Color.purple };

        void OnProgress(int teamId, float progress)
        {
            var flagPos = flag.transform.localPosition;
            flagPos.y = progress * 3f;
            flag.transform.localPosition = flagPos;

            flag.color = flagColors[teamId];
        }
    }

    public interface IEventZone
    {
        void ExecuteEvent(int index);
    }
}
