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

    [Tooltip("적 스폰 Y 위치. 프리팹 피벗이 발밑이면 0.")]
    public float spawnY = 0f;

    [Header("Boss")]
    public GameObject bossPrefab;
    public float bossSpawnDelay = 2f;

    [Tooltip("보스 스폰 X 위치")]
    public float bossSpawnX = 0f;

    [Tooltip("보스 스폰 Y 위치. 프리팹 피벗이 발밑이면 0.")]
    public float bossSpawnY = 0f;

    [Tooltip("보스 스폰 Z 위치 (화면 위쪽). 여기서 등장해 Boss.battleZ로 내려옴.")]
    public float bossSpawnZ = 20f;

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

        if (GameManager.Instance != null)
            GameManager.Instance.NotifyAllWavesCleared();

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
            StartCoroutine(ScheduleDesignedSpawn(entry, waveStartTime));

        foreach (var rule in wave.randomSpawns)
            StartCoroutine(ScheduleRandomSpawn(rule, waveStartTime));

        float lastSpawnTime = 0f;
        foreach (var entry in wave.spawns)
        {
            float endTime = entry.spawnTime + (entry.count - 1) * entry.spawnDelay;
            if (endTime > lastSpawnTime) lastSpawnTime = endTime;
        }
        foreach (var rule in wave.randomSpawns)
        {
            float endTime = rule.startTime + rule.duration;
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

    IEnumerator ScheduleDesignedSpawn(WaveData.SpawnEntry entry, float waveStartTime)
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
            SpawnAt(prefab, entry.xPosition + xOffset);

            if (i < entry.count - 1 && entry.spawnDelay > 0f)
                yield return new WaitForSeconds(entry.spawnDelay);
        }
    }

    IEnumerator ScheduleRandomSpawn(WaveData.RandomSpawnRule rule, float waveStartTime)
    {
        float wait = (waveStartTime + rule.startTime) - Time.time;
        if (wait > 0f) yield return new WaitForSeconds(wait);

        GameObject prefab = rule.enemyPrefabOverride != null
            ? rule.enemyPrefabOverride
            : defaultEnemyPrefab;

        if (prefab == null || rule.totalCount <= 0) yield break;

        float baseInterval = rule.duration / rule.totalCount;

        for (int i = 0; i < rule.totalCount; i++)
        {
            float x = Random.Range(rule.minX, rule.maxX);
            SpawnAt(prefab, x);

            if (i < rule.totalCount - 1)
            {
                float jitter = 1f + Random.Range(-rule.intervalJitter, rule.intervalJitter);
                yield return new WaitForSeconds(baseInterval * jitter);
            }
        }
    }

    void SpawnAt(GameObject prefab, float xPosition)
    {
        Vector3 pos = new Vector3(xPosition, spawnY, spawnZ);

        // 풀에서 꺼냄 (없으면 fallback). Get 시 Enemy.OnSpawn이 상태를 리셋함.
        GameObject enemyGo = PoolManager.Instance != null
            ? PoolManager.Instance.Get(prefab)
            : Instantiate(prefab);
        if (enemyGo == null) return;

        Enemy enemy = enemyGo.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.Spawn(pos, Quaternion.identity);   // transform + rb.position 즉시 동기화
            aliveEnemies++;
            enemy.OnDied += HandleEnemyDied;
        }
        else
        {
            enemyGo.transform.SetPositionAndRotation(pos, Quaternion.identity);
            Debug.LogWarning($"[WaveSpawner] {prefab.name}에 Enemy 컴포넌트가 없음", prefab);
        }
    }

    void HandleEnemyDied(Enemy enemy)
    {
        enemy.OnDied -= HandleEnemyDied;
        aliveEnemies = Mathf.Max(0, aliveEnemies - 1);
    }

    void SpawnBoss()
    {
        // 보스는 스테이지당 1회만 등장하고 등장연출/static 참조 등 고유 상태가 있어
        // 풀링 이득이 거의 없음 → 의도적으로 Instantiate 유지.
        Vector3 pos = new Vector3(bossSpawnX, bossSpawnY, bossSpawnZ);
        // -Z(플레이어 쪽)를 보게 회전
        Quaternion rot = Quaternion.LookRotation(Vector3.back);
        Instantiate(bossPrefab, pos, rot);
        Debug.Log("[Boss] 등장!");
    }
}