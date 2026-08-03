using System;
using UnityEngine;

// 맵 파일의 id ↔ 프리팹 대응표. 외부 생성기는 이 id만 알면 된다.
[CreateAssetMenu(fileName = "MapCatalog", menuName = "혹한/Map Catalog")]
public class MapCatalog : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        [Tooltip("맵 파일에 쓰는 이름 (예: hearth, tree, stone, hearth_site)")]
        public string id;
        public GameObject prefab;
    }

    public Entry[] entries;

    public GameObject Find(string id)
    {
        if (entries == null || string.IsNullOrEmpty(id)) return null;
        for (int i = 0; i < entries.Length; i++)
            if (entries[i].id == id) return entries[i].prefab;
        return null;
    }
}
