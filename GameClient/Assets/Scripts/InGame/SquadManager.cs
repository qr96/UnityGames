using System.Collections.Generic;
using UnityEngine;

namespace InGame
{
    public class SquadManager : MonoBehaviour
    {
        public GameObject soldierPrefab;
        public int totalSoldiers = 9;

        void Start()
        {
            SpawnSquad();
        }

        void SpawnSquad()
        {
            for (int i = 0; i < totalSoldiers; i++)
            {
                Instantiate(soldierPrefab, transform.position, Quaternion.identity);
            }
        }
    }
}
