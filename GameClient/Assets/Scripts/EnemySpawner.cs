using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public float spawnInterval = 1f;
    public float spawnZ = 30f;
    public float spawnRangeX = 4f;

    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            Vector3 pos = new Vector3(Random.Range(-spawnRangeX, spawnRangeX), 1f, spawnZ);
            Instantiate(enemyPrefab, pos, Quaternion.identity);
            timer = 0f;
        }
    }
}