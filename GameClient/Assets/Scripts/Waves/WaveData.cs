using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Runner/Wave Data", fileName = "NewWave")]
public class WaveData : ScriptableObject
{
    [System.Serializable]
    public class SpawnEntry
    {
        [Tooltip("웨이브 시작 후 몇 초 뒤에 스폰")]
        public float spawnTime = 0f;

        [Tooltip("스폰할 적 프리팹 (비우면 WaveSpawner 기본값)")]
        public GameObject enemyPrefabOverride;

        public float xPosition = 0f;

        [Tooltip("같은 시점에 몇 마리")]
        public int count = 1;

        [Tooltip("마리 사이의 X 간격")]
        public float spacing = 1.5f;

        [Tooltip("연속 스폰 시 마리 사이 시간 간격")]
        public float spawnDelay = 0f;
    }

    [System.Serializable]
    public class RandomSpawnRule
    {
        [Tooltip("이 규칙이 작동할 적 프리팹 (비우면 WaveSpawner 기본값)")]
        public GameObject enemyPrefabOverride;

        [Tooltip("웨이브 시작 후 몇 초에 랜덤 스폰 시작")]
        public float startTime = 0f;

        [Tooltip("랜덤 스폰이 지속되는 시간")]
        public float duration = 10f;

        [Tooltip("이 규칙으로 총 몇 마리를 스폰할지")]
        public int totalCount = 5;

        [Tooltip("스폰 가능한 X 좌표 범위")]
        public float minX = -4f;
        public float maxX = 4f;

        [Tooltip("스폰 간격 흔들림 (0=균등, 1=0~2배 사이)")]
        [Range(0f, 1f)] public float intervalJitter = 0.3f;
    }

    [Header("Designed Spawns")]
    public List<SpawnEntry> spawns = new List<SpawnEntry>();

    [Header("Random Spawns")]
    public List<RandomSpawnRule> randomSpawns = new List<RandomSpawnRule>();

    [Header("Wave Settings")]
    public float maxDuration = 30f;
    public float restAfter = 2f;
}