using InGameModel;
using System.Collections.Generic;
using UnityEngine;

namespace InGame
{
    public class SquadManager : MonoBehaviour
    {
        public List<GameObject> soldiers;

        Dictionary<int, LeaderUnit> leaderDic = new Dictionary<int, LeaderUnit>();

        public void AddTeam(int teamId, LeaderUnit leader)
        {
            leaderDic.Add(teamId, leader);
        }

        public void SpawnSquad(int teamId, int spawnCount, Vector2 spawnPos, string unitCode)
        {
            if (!leaderDic.ContainsKey(teamId))
            {
                Debug.LogError("Leader is not exist");
                return;
            }

            var leader = leaderDic[teamId];
            var commander = leader.GetComponent<MinionFormationCommander>();

            for (int i = 0; i < spawnCount; i++)
            {
                //var unit = Instantiate(soldiers[soldierCode], transform.position, Quaternion.identity);
                if (PoolManager.Instance.TryCreate($"Prefabs/Units/{unitCode}", out var unit))
                {
                    var soldier = unit.GetComponent<SoldierUnit>();
                    if (soldier != null)
                    {
                        soldier.gameObject.SetActive(true);
                        soldier.TeamId = teamId;
                        soldier.SetLeader(leader.GetComponent<Rigidbody2D>());
                        soldier.transform.position = spawnPos;
                        soldier.SetColor(GameUtil.GetTeamUnitColor(teamId));
                        soldier.SetModel(new UnitModel() { maxHp = 10, attack = 2 });
                        soldier.OnSpawn();

                        if (commander != null)
                            commander.AddMinion(soldier);
                    }
                }
            }

            if (commander != null)
                commander.SetMinionsPosition(leader.transform.position, Vector2.right);
            else
                Debug.LogError($"{leader.name} Failed to find MinionFormationCommander script.");
        }
    }
}
