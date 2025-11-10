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
                var randomPos = new Vector3(Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f));
                SpawnMonster(randomPos);
            }
        }

        void SpawnMonster(Vector3 position)
        {
            if (Managers.Instance.GetComponent<GameObjectManager>().TryCreate("Monster/001", out var go))
            {
                go.transform.position = position;
                //go.GetComponent<EnemyController>()
            }
        }
    }
}
