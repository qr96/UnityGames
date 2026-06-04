using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 프리팹 직접 참조 기반 오브젝트 풀 매니저.
///
/// 키 = 원본 프리팹(GameObject). 호출부가 이미 들고 있는 프리팹 참조를 그대로
/// Get(prefab)에 넘기면 되므로 문자열 경로/Resources.Load/오타가 전부 사라짐.
///
/// 사용:
///   GameObject e = PoolManager.Instance.Get(enemyPrefab);   // 꺼내기
///   PoolManager.Instance.Release(enemyPrefab, e);           // 반납 (보통 Poolable.ReleaseSelf 사용)
/// </summary>
public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance;

    [Header("Configuration")]
    [Tooltip("풀 설정이 담긴 ScriptableObject를 연결하세요.")]
    [SerializeField] private PoolManagerConfig config;

    // 풀들을 관리하는 딕셔너리 (Key: 원본 프리팹)
    private readonly Dictionary<GameObject, IObjectPool<GameObject>> poolDic
        = new Dictionary<GameObject, IObjectPool<GameObject>>();

    // 풀별 부모 트랜스폼 (하이어라키 정리용)
    private readonly Dictionary<GameObject, Transform> poolGroups
        = new Dictionary<GameObject, Transform>();

    // 인스턴스 → 원본 프리팹 역참조 (반납/식별용 안전망)
    private readonly Dictionary<GameObject, GameObject> instanceToPrefab
        = new Dictionary<GameObject, GameObject>();

    private Transform _root;
    private int id;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);   // 매니저와 Root 수명 일치

            _root = new GameObject("@Pool_Root").transform;
            DontDestroyOnLoad(_root.gameObject);

            InitializePoolsFromConfig();
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    /// <summary>연결된 SO에서 모든 풀 설정을 읽어와 초기화 + 프리워밍.</summary>
    private void InitializePoolsFromConfig()
    {
        if (config == null)
        {
            Debug.LogError("[PoolManager] PoolManagerConfig가 연결되지 않았습니다! 풀이 생성되지 않습니다.");
            return;
        }

        foreach (var setting in config.poolSettings)
        {
            if (setting.prefab == null)
            {
                Debug.LogWarning("[PoolManager] 프리팹이 비어있는 설정이 있습니다. 건너뜁니다.");
                continue;
            }

            CreatePool(setting.prefab, setting.maxSize);
            Prewarm(setting.prefab, setting.prewarmCount);
        }

        Debug.Log($"[PoolManager] {config.poolSettings.Count}개의 풀 설정을 로드했습니다.");
    }

    // ==================================================================================
    // 외부 접근 함수
    // ==================================================================================

    /// <summary>
    /// 프리팹에 해당하는 풀에서 오브젝트를 꺼낸다.
    /// 등록되지 않은 프리팹이면 기본값으로 풀을 자동 생성한다.
    /// </summary>
    public GameObject Get(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("[PoolManager] Get에 null 프리팹이 전달됐습니다.");
            return null;
        }

        if (!poolDic.ContainsKey(prefab))
        {
            // SO에 등록 안 된 프리팹 → 경고 후 기본값(maxSize 100)으로 자동 생성
            Debug.LogWarning($"[PoolManager] '{prefab.name}'은 설정에 없습니다. 임시 풀을 생성합니다.");
            CreatePool(prefab, 100);
        }

        return poolDic[prefab].Get();
    }

    /// <summary>사용이 끝난 오브젝트를 반납한다. 보통 Poolable.ReleaseSelf가 호출.</summary>
    public void Release(GameObject prefab, GameObject obj)
    {
        if (obj == null) return;

        // 키가 비어 들어오면 역참조로 보정
        if (prefab == null) instanceToPrefab.TryGetValue(obj, out prefab);

        if (prefab != null && poolDic.ContainsKey(prefab))
        {
            poolDic[prefab].Release(obj);
        }
        else
        {
            // 풀을 못 찾으면 안전하게 파괴
            Destroy(obj);
        }
    }

    // ==================================================================================
    // 내부 헬퍼
    // ==================================================================================

    /// <summary>풀을 생성하고 딕셔너리에 등록. 이미 있으면 무시.</summary>
    private void CreatePool(GameObject prefab, int maxSize)
    {
        if (poolDic.ContainsKey(prefab)) return;

        Transform poolGroup = new GameObject($"{prefab.name}_Pool").transform;
        poolGroup.SetParent(_root);
        poolGroups[prefab] = poolGroup;

        IObjectPool<GameObject> pool = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                var obj = Instantiate(prefab, poolGroup);
                obj.name = $"{prefab.name} {id++}";

                var poolable = obj.GetComponent<Poolable>();
                if (poolable == null) poolable = obj.AddComponent<Poolable>();
                poolable.poolKey = prefab;   // 키 = 원본 프리팹

                instanceToPrefab[obj] = prefab;
                return obj;
            },
            actionOnGet: (obj) =>
            {
                obj.SetActive(true);
                // 재사용 상태 리셋 훅
                if (obj.TryGetComponent(out IPoolable p)) p.OnSpawn();
            },
            actionOnRelease: (obj) =>
            {
                if (obj.TryGetComponent(out IPoolable p)) p.OnDespawn();
                obj.SetActive(false);
                obj.transform.SetParent(poolGroup, false);  // 월드 변환 재계산 회피
            },
            actionOnDestroy: (obj) =>
            {
                instanceToPrefab.Remove(obj);
                Destroy(obj);
            },
            collectionCheck: true,
            defaultCapacity: Mathf.Max(1, maxSize / 2),
            maxSize: maxSize
        );

        poolDic.Add(prefab, pool);
    }

    /// <summary>
    /// 실제로 인스턴스를 미리 생성해 풀에 채운다.
    /// (유니티 ObjectPool의 defaultCapacity는 내부 스택 용량일 뿐 인스턴스를 만들지 않음.
    ///  그래서 첫 스폰 히치를 막으려면 이렇게 직접 워밍해야 함.)
    /// </summary>
    private void Prewarm(GameObject prefab, int count)
    {
        if (count <= 0 || !poolDic.ContainsKey(prefab)) return;

        var pool = poolDic[prefab];
        var temp = new List<GameObject>(count);
        for (int i = 0; i < count; i++) temp.Add(pool.Get());
        for (int i = 0; i < temp.Count; i++) pool.Release(temp[i]);
    }

    /// <summary>모든 풀을 비우고 메모리를 해제한다.</summary>
    public void Clear()
    {
        foreach (var pool in poolDic.Values) pool.Clear();
        poolDic.Clear();
        poolGroups.Clear();
        instanceToPrefab.Clear();

        Debug.Log("[PoolManager] Clear()");
    }
}