using System.Collections.Generic;
using UnityEngine;

namespace InGame
{
    public class SquadManager : MonoBehaviour
    {
        public Rigidbody2D playerA;
        public Rigidbody2D playerB;

        public GameObject soldierPrefab;
        public int totalSoldiers = 9;

        void Start()
        {
            SpawnSquad(1, playerA);
            //SpawnSquad(2, playerB);
        }

        void SpawnSquad(int teamId, Rigidbody2D leader)
        {
            var commander = leader.GetComponent<MinionFormationCommander>();

            for (int i = 0; i < totalSoldiers; i++)
            {
                var unit = Instantiate(soldierPrefab, transform.position, Quaternion.identity);
                var soldier = unit.GetComponent<SoldierUnit>();
                if (soldier != null)
                {
                    soldier.gameObject.SetActive(true);
                    soldier.TeamId = teamId;
                    soldier.SetLeader(leader);
                    soldier.transform.position = leader.position;

                    if (commander != null)
                        commander.AddMinion(soldier);
                }
            }

            if (commander != null)
                commander.SetMinionsPosition(leader.position, Vector2.up);
            else
                Debug.LogError($"{leader.name} Failed to find MinionFormationCommander script.");
        }
    }
}
