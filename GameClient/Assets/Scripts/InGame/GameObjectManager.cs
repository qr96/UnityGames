using System.Collections.Generic;
using UnityEngine;

public class GameObjectManager : MonoBehaviour
{
    public List<ResourceMeta<GameObject>> metaList = new List<ResourceMeta<GameObject>>();

    Dictionary<string, Queue<GameObject>> poolDic = new Dictionary<string, Queue<GameObject>>();
    Dictionary<string, ResourceMeta<GameObject>> metaDic = new Dictionary<string, ResourceMeta<GameObject>>();

    readonly string prefabPath = "Prefab/";

    private void Awake()
    {
        InitializeMeta();
    }

    public bool TryCreate(string name, out GameObject item)
    {
        item = null;

        if (!poolDic.ContainsKey(name))
        {
            Debug.LogError($"Create() No Key in poolDic. name={name}");
            return false;
        }
        else if (!metaDic.ContainsKey(name))
        {
            Debug.LogError($"Create() No Key in metaDic. name={name}");
            return false;
        }

        // 풀에서 찾고 없으면 생성
        GameObject go = null;

        if (poolDic[name].Count < metaDic[name].maxSize)
        {
            go = Instantiate(metaDic[name].resource);
            go.name = $"{name} {poolDic[name].Count}";
        }
        else
            go = poolDic[name].Dequeue();

        if (go != null)
        {
            poolDic[name].Enqueue(go);
            go.SetActive(false);
            go.SetActive(true);
        }
        else
            Debug.LogError($"TryCreate() Failed to get go={go.name}");

        // item에 할당 및 true 반환
        item = go;
        return true;
    }

    // 리소스 로드, Pool 및 Meta 딕셔너리 초기화
    void InitializeMeta()
    {
        foreach (var meta in metaList)
        {
            var fullPath = prefabPath + meta.name;
            var prefab = Resources.Load<GameObject>(fullPath);

            meta.resource = prefab;
            poolDic.Add(meta.name, new Queue<GameObject>());
            metaDic.Add(meta.name, meta);
        }
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
