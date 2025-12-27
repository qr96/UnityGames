using System.Collections.Generic;
using UnityEngine;

namespace InGame
{
    public class SquadManager : MonoBehaviour
    {
        public LeaderUnit playerA;
        public LeaderUnit playerB;

        public GameObject base1;
        public GameObject base2;

        public GameObject soldierPrefab;

        void Start()
        {
            //SpawnSquad(1, playerA);
            SpawnSquad(2, playerB, 9, base2.transform.position);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                var price = 20;

                if (InGamePropertyManager.Instance.UseFood(1, price))
                    SpawnSquad(1, playerA, 1, base1.transform.position);
                else
                    Debug.Log("Not enough money.");
            }
        }

        void SpawnSquad(int teamId, LeaderUnit leader, int spawnCount, Vector2 spawnPos)
        {
            var commander = leader.GetComponent<MinionFormationCommander>();
            leader.TeamId = teamId;

            for (int i = 0; i < spawnCount; i++)
            {
                var unit = Instantiate(soldierPrefab, transform.position, Quaternion.identity);
                var soldier = unit.GetComponent<SoldierUnit>();
                if (soldier != null)
                {
                    soldier.gameObject.SetActive(true);
                    soldier.TeamId = teamId;
                    soldier.SetLeader(leader.GetComponent<Rigidbody2D>());
                    soldier.transform.position = spawnPos;

                    if (teamId == 2)
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
    }
}
