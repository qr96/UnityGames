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

    public List<SpawnEntry> spawns = new List<SpawnEntry>();

    [Tooltip("모든 적이 죽거나 이 시간 지나면 다음 웨이브")]
    public float maxDuration = 30f;

    [Tooltip("다음 웨이브로 넘어가기 전 휴식")]
    public float restAfter = 2f;
}
