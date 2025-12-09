using UnityEngine;

namespace InGame
{
    public class MonsterSpawner : MonoBehaviour
    {
        public Enemy monsterPrefab;

        public Vector2Int size = new Vector2Int(10, 8);
        public Vector3 startPos = new Vector3(-5f, -0.5f, -4f);
        public Vector3 gap = new Vector3(1.2f, 0f, 1.2f);
        public float spawnPercent = 0.8f;

        private void Start()
        {
            Spawn();
        }

        void Spawn()
        {
            for (int i = 0; i < size.x; i++)
            {
                for (int j = 0; j < size.y; j++)
                {
                    var rand = Random.Range(0f, 1f);
                    if (rand < spawnPercent)
                    {
                        if (PoolManager.Instance.TryCreate("Monsters/Monster0", out var monsterGo))
                        {
                            var monster = monsterGo.GetComponent<Enemy>();
                            monster.Spawn(new GameModel.UnitModel() { maxHp = 20 });
                            monster.gameObject.SetActive(true);
                            monster.transform.position = startPos + new Vector3(i * gap.x, 0f, j * gap.z);
                        }
                    }
                }
            }
        }
    }
}

