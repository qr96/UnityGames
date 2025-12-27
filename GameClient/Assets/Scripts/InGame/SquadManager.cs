using System.Collections.Generic;
using UnityEngine;

namespace InGame
{
    public class SquadManager : MonoBehaviour
    {
        public LeaderUnit playerA;
        public LeaderUnit playerB;
        public LeaderUnit playerC;

        public List<GameObject> bases;

        public List<GameObject> soldiers;

        void Start()
        {
            SpawnSquad(1, playerB, 9, GetBaseCamp(1).transform.position, 0);
            SpawnSquad(2, playerC, 5, GetBaseCamp(2).transform.position, 1);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                var price = 20;
                var teamId = 0;

                if (InGamePropertyManager.Instance.UseFood(1, price))
                    SpawnSquad(teamId, playerA, 1, GetBaseCamp(teamId).transform.position, 0);
                else
                    Debug.Log("Not enough money.");
            }
        }

        void SpawnSquad(int teamId, LeaderUnit leader, int spawnCount, Vector2 spawnPos, int soldierCode)
        {
            var commander = leader.GetComponent<MinionFormationCommander>();
            leader.TeamId = teamId;

            for (int i = 0; i < spawnCount; i++)
            {
                var unit = Instantiate(soldiers[soldierCode], transform.position, Quaternion.identity);
                var soldier = unit.GetComponent<SoldierUnit>();
                if (soldier != null)
                {
                    soldier.gameObject.SetActive(true);
                    soldier.TeamId = teamId;
                    soldier.SetLeader(leader.GetComponent<Rigidbody2D>());
                    soldier.transform.position = spawnPos;

                    if (teamId != 0)
                        soldier.SetColor(new Color(1f, 180f / 255f, 180f / 255f));

                    if (commander != null)
                        commander.AddMinion(soldier);
                }
            }

            if (commander != null)
                commander.SetMinionsPosition(leader.transform.position, Vector2.right);
            else
                Debug.LogError($"{leader.name} Failed to find MinionFormationCommander script.");
        }

        GameObject GetBaseCamp(int teamId)
        {
            return bases[teamId];
        }
    }
}
