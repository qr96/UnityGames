using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance;

    // ScriptableObject 의존성 주입: 인스펙터에서 SO 파일을 연결해야 합니다.
    [Header("Configuration")]
    [Tooltip("풀 설정이 담긴 ScriptableObject를 연결하세요.")]
    [SerializeField] private PoolManagerConfig config;

    // 풀들을 관리하는 딕셔너리 (Key: Resources 경로)
    private Dictionary<string, IObjectPool<GameObject>> poolDic = new Dictionary<string, IObjectPool<GameObject>>();

    // 하이어라키 창 정리를 위한 최상위 부모 트랜스폼
    private Transform _root;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);

            _root = new GameObject("@Pool_Root").transform;
            DontDestroyOnLoad(_root.gameObject);

            InitializePoolsFromConfig();
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    /// <summary>
    /// 연결된 ScriptableObject에서 모든 풀 설정을 읽어와 초기화합니다.
    /// </summary>
    private void InitializePoolsFromConfig()
    {
        if (config == null)
        {
            Debug.LogError("[PoolManager] PoolManagerConfig ScriptableObject가 연결되지 않았습니다! 풀이 생성되지 않습니다.");
            return;
        }

        foreach (var setting in config.poolSettings)
        {
            if (string.IsNullOrEmpty(setting.resourcePath))
            {
                Debug.LogWarning("[PoolManager] 경로가 비어있는 설정이 있습니다. 건너뜁니다.");
                continue;
            }

            // SO에 정의된 maxCapacity와 maxSize로 풀 생성
            CreatePool(setting.resourcePath, setting.defaultCapacity, setting.maxSize);
        }

        Debug.Log($"[PoolManager] {config.poolSettings.Count}개의 풀 설정을 로드하여 초기화했습니다.");
    }

    // ==================================================================================
    // 외부 접근 함수
    // ==================================================================================

    /// <summary>
    /// 오브젝트를 풀에서 가져옵니다. (SO에 등록되지 않은 오브젝트는 경고 후 기본값으로 생성)
    /// </summary>
    public bool TryCreate(string path, out GameObject obj)
    {
        if (!poolDic.ContainsKey(path))
        {
            // SO에 등록되지 않은 오브젝트를 호출하면 경고 후 기본값으로 풀 생성
            Debug.LogWarning($"[PoolManager] '{path}'는 설정 파일에 없습니다. 임시로 기본값(10/100) 풀을 생성합니다.");
            if (CreatePool(path, 10, 100) == false)
            {
                obj = null;
                return false;
            }
        }

        obj = poolDic[path].Get();
        obj.gameObject.SetActive(true);

        return true;
    }

    /// <summary>
    /// 사용이 끝난 오브젝트를 풀에 반납합니다.
    /// </summary>
    public void Release(string path, GameObject obj)
    {
        if (poolDic.ContainsKey(path))
        {
            poolDic[path].Release(obj);
        }
        else
        {
            Destroy(obj);
        }
    }

    // ==================================================================================
    // 내부 헬퍼 함수
    // ==================================================================================

    /// <summary>
    /// 내부적으로 풀을 생성하고 딕셔너리에 등록합니다.
    /// </summary>
    private bool CreatePool(string path, int defaultCapacity, int maxSize)
    {
        GameObject prefab = Resources.Load<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogError($"[PoolManager] 프리팹을 찾을 수 없습니다. 경로를 확인하세요: Resources/{path}");
            return false;
        }

        Transform poolGroup = new GameObject($"{path}_Pool").transform;
        poolGroup.SetParent(_root);

        // 유니티 내장 ObjectPool 생성
        IObjectPool<GameObject> pool = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                var obj = Instantiate(prefab, poolGroup);
                obj.name = prefab.name;

                var poolable = obj.GetComponent<Poolable>();
                if (poolable == null)
                {
                    poolable = obj.AddComponent<Poolable>();
                }
                poolable.poolKey = path; // 이 오브젝트의 키를 저장

                return obj;
            },
            actionOnGet: (obj) => obj.SetActive(true),
            actionOnRelease: (obj) =>
            {
                obj.SetActive(false);
                obj.transform.SetParent(poolGroup);
            },
            actionOnDestroy: (obj) => Destroy(obj),
            collectionCheck: true,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize
        );

        poolDic.Add(path, pool);
        return true;
    }

    /// <summary>
    /// 씬이 로드될 때 호출되어 모든 풀을 초기화합니다.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Clear();
        InitializePoolsFromConfig(); // 새로운 씬 로드 후 풀 재초기화
    }

    /// <summary>
    /// 모든 풀을 비우고 메모리 누수를 방지합니다.
    /// </summary>
    public void Clear()
    {
        foreach (var pool in poolDic.Values) pool.Clear();
        poolDic.Clear();

        if (_root != null)
        {
            Destroy(_root.gameObject); // C++ Native Memory 해제
            _root = new GameObject("@Pool_Root").transform; // 새 Root 생성
            DontDestroyOnLoad(_root.gameObject);
        }

        Debug.Log("[PoolManager] Clear()");
    }
}

[System.Serializable]
public class ResourceMeta<T>
{
    public string name;
    public int maxSize;

    // 이 필드는 로드 후 임시로 프리팹을 저장합니다.
    [HideInInspector] public T resource;
}
