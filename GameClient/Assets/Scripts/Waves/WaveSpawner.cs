using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    public static WaveSpawner Instance { get; private set; }

    [Header("Setup")]
    public GameObject defaultEnemyPrefab;
    public List<WaveData> waves = new List<WaveData>();
    public float spawnZ = 30f;
    public float startDelay = 1.5f;

    [Header("Boss")]
    public GameObject bossPrefab;
    public float bossSpawnDelay = 2f;

    public int CurrentWaveIndex { get; private set; } = -1;
    public bool AllWavesCleared { get; private set; }

    private int aliveEnemies = 0;

    public System.Action<int> OnWaveStarted;
    public System.Action<int> OnWaveCleared;
    public System.Action OnAllWavesCleared;

    void Awake() { Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; }

    void Start()
    {
        StartCoroutine(RunWaves());
    }

    IEnumerator RunWaves()
    {
        yield return new WaitForSeconds(startDelay);

        for (int i = 0; i < waves.Count; i++)
        {
            CurrentWaveIndex = i;
            yield return StartCoroutine(RunSingleWave(waves[i], i + 1));
        }

        AllWavesCleared = true;
        OnAllWavesCleared?.Invoke();

        if (bossPrefab != null)
        {
            yield return new WaitForSeconds(bossSpawnDelay);
            SpawnBoss();
        }
    }

    IEnumerator RunSingleWave(WaveData wave, int waveNumber)
    {
        OnWaveStarted?.Invoke(waveNumber);
        Debug.Log($"[Wave] {waveNumber} 시작");

        float waveStartTime = Time.time;
        foreach (var entry in wave.spawns)
            StartCoroutine(ScheduleSpawn(entry, waveStartTime));

        float lastSpawnTime = 0f;
        foreach (var entry in wave.spawns)
        {
            float endTime = entry.spawnTime + (entry.count - 1) * entry.spawnDelay;
            if (endTime > lastSpawnTime) lastSpawnTime = endTime;
        }

        yield return new WaitForSeconds(lastSpawnTime + 0.5f);

        while (aliveEnemies > 0)
        {
            if (Time.time - waveStartTime > wave.maxDuration) break;
            yield return null;
        }

        OnWaveCleared?.Invoke(waveNumber);
        Debug.Log($"[Wave] {waveNumber} 클리어");

        yield return new WaitForSeconds(wave.restAfter);
    }

    IEnumerator ScheduleSpawn(WaveData.SpawnEntry entry, float waveStartTime)
    {
        float wait = (waveStartTime + entry.spawnTime) - Time.time;
        if (wait > 0f) yield return new WaitForSeconds(wait);

        GameObject prefab = entry.enemyPrefabOverride != null
            ? entry.enemyPrefabOverride
            : defaultEnemyPrefab;

        if (prefab == null) yield break;

        for (int i = 0; i < entry.count; i++)
        {
            float xOffset = (i - (entry.count - 1) * 0.5f) * entry.spacing;
            Vector3 pos = new Vector3(entry.xPosition + xOffset, 1f, spawnZ);
            GameObject enemyGo = Instantiate(prefab, pos, Quaternion.identity);

            Enemy enemy = enemyGo.GetComponent<Enemy>();
            if (enemy != null)
            {
                aliveEnemies++;
                enemy.OnDied += HandleEnemyDied;
            }
            else
            {
                Debug.LogWarning($"[WaveSpawner] {prefab.name}에 Enemy 컴포넌트가 없음", prefab);
            }

            if (i < entry.count - 1 && entry.spawnDelay > 0f)
                yield return new WaitForSeconds(entry.spawnDelay);
        }
    }

    void HandleEnemyDied(Enemy enemy)
    {
        enemy.OnDied -= HandleEnemyDied;
        aliveEnemies = Mathf.Max(0, aliveEnemies - 1);
    }

    void SpawnBoss()
    {
        Vector3 pos = new Vector3(0f, 1f, spawnZ - 10f);
        Instantiate(bossPrefab, pos, Quaternion.identity);
        Debug.Log("[Boss] 등장!");
    }
}
