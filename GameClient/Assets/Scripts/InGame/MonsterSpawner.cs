using UnityEngine;

namespace InGame
{
    public class MonsterSpawner : MonoBehaviour
    {
        public int maxCount = 5;
        public float spawnDelay = 30f;

        float nextSpawnTime;

        private void Update()
        {
            if (Time.time > nextSpawnTime)
            {
                SpawnMonsters();
                nextSpawnTime = Time.time + spawnDelay;
            }
        }

        void SpawnMonsters()
        {
            while (Managers.Instance.GetComponent<MonsterManager>().GetActiveMonsterCount() < maxCount)
            {
                var randomPos = new Vector3(Random.Range(-15f, 15f), 0f, Random.Range(-15f, 15f));
                if (!TrySpawnMonster(randomPos))
                {
                    Debug.LogError("[MonsterSpawner] Failed to SpawnMonster");
                    break;
                }
            }
        }

        bool TrySpawnMonster(Vector3 position)
        {
            if (PoolManager.Instance.TryCreate("Prefab/Monster/001", out var go))
            {
                go.transform.position = position;
                return true;
            }

            return false;
        }
    }
}
