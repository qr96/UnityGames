using System.Collections.Generic;
using UnityEngine;

namespace InGame
{
    public class SquadManager : MonoBehaviour
    {
        public LeaderUnit playerA;
        public LeaderUnit playerB;

        public GameObject soldierPrefab;
        public int totalSoldiers = 9;

        void Start()
        {
            SpawnSquad(1, playerA);
            SpawnSquad(2, playerB);
        }

        void SpawnSquad(int teamId, LeaderUnit leader)
        {
            var commander = leader.GetComponent<MinionFormationCommander>();
            leader.TeamId = teamId;

            for (int i = 0; i < totalSoldiers; i++)
            {
                var unit = Instantiate(soldierPrefab, transform.position, Quaternion.identity);
                var soldier = unit.GetComponent<SoldierUnit>();
                if (soldier != null)
                {
                    soldier.gameObject.SetActive(true);
                    soldier.TeamId = teamId;
                    soldier.SetLeader(leader.GetComponent<Rigidbody2D>());
                    soldier.transform.position = leader.transform.position;

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
