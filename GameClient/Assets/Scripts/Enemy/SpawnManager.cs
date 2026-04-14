using UnityEngine;

/// <summary>
/// 적 스폰 관리.
/// 씬에 SpawnPoint 오브젝트들을 배치하고 이 매니저에 연결.
/// </summary>
public class SpawnManager : MonoBehaviour
{
    [System.Serializable]
    public struct SpawnEntry
    {
        public EnemyData data;
        public string prefabPath;   // Resources 경로 (예: "Enemy/Goblin")
        public Transform[] points;  // 스폰 위치들
    }

    [SerializeField] SpawnEntry[] _entries;

    void Start()
    {
        SpawnAll();
    }

    public void SpawnAll()
    {
        foreach (var entry in _entries)
            foreach (var point in entry.points)
                Spawn(entry.data, entry.prefabPath, point.position);
    }

    public void Spawn(EnemyData data, string prefabPath, Vector3 position)
    {
        if (!PoolManager.Instance.TryCreate(prefabPath, out var obj)) return;

        obj.transform.position = position;
        obj.GetComponent<Enemy>().Init(data);
    }
}