using InGame;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InGame
{
    public class FieldManager : MonoBehaviour
    {
        public static FieldManager Instance;

        public SquadManager squad;

        public List<LeaderUnit> leaders = new List<LeaderUnit>();
        public List<CapturePoint> capturePoints = new List<CapturePoint>();

        Dictionary<int, InGamePropertyManager> propertyDic = new Dictionary<int, InGamePropertyManager>();

        int teamIdCounter = 1;
        Coroutine produceFoodCo;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(this);

            // Settings
            for (int i = 0; i < capturePoints.Count; i++)
            {
                capturePoints[i].PointId = i + 1;
                capturePoints[i].OnChangeOwner += OnChangeCapturePoint;
            }

            for (int i = 0; i < leaders.Count; i++)
                AddTeam(teamIdCounter++, 100, leaders[i]);

            if (produceFoodCo != null)
                StopCoroutine(produceFoodCo);
            produceFoodCo = StartCoroutine(ProduceFoodCo());

            // Tests
            squad.SpawnSquad(2, 10, leaders[1].transform.position, "001");
            squad.SpawnSquad(3, 5, leaders[2].transform.position, "002");
        }

        private void OnDestroy()
        {
            foreach (var point in capturePoints)
                point.OnChangeOwner -= OnChangeCapturePoint;
        }

        public void AddTeam(int teamId, long currentFood, LeaderUnit leader)
        {
            leader.TeamId = teamId;
            propertyDic.Add(teamId, new InGamePropertyManager(currentFood));
            squad.AddTeam(teamId, leader);
        }

        public InGamePropertyManager GetInGameProperty(int teamId)
        {
            if (propertyDic.ContainsKey(teamId))
                return propertyDic[teamId];
            else if (teamId == 0)
                return null;

            Debug.LogError($"Cant Find teamId={teamId}");
            return null;
        }

        public bool TryGetProperty(int teamId, out InGamePropertyManager property)
        {
            if (propertyDic.ContainsKey(teamId))
            {
                property = propertyDic[teamId];
                return true;
            }
            else if (teamId != 0)
            {
                Debug.LogError($"[TryGetProperty] Cant Find Property for teamId={teamId}");
            }

            property = null;
            return false;
        }

        public bool TryProduceUnit(int teamId, string unitCode, Vector2 spawnPos)
        {
            if (TryGetProperty(teamId, out var property))
            {
                var price = 20;
                if (property.TryUseFood(price))
                {
                    squad.SpawnSquad(teamId, 1, spawnPos, unitCode);
                    return true;
                }
            }

            return false;
        }

        void OnChangeCapturePoint(CapturePoint point, int prevTeamId, int nowOwnTeamId)
        {
            if (TryGetProperty(prevTeamId, out var propertyPrev))
                propertyPrev.foodProduction -= point.foodProduction;
            if (TryGetProperty(nowOwnTeamId, out var propertyNow))
                propertyNow.foodProduction += point.foodProduction;
        }

        IEnumerator ProduceFoodCo()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                foreach (var property in propertyDic.Values)
                    property.ProduceFood();
            }
            
        }
    }
}
