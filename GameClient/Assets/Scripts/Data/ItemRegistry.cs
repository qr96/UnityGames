using System.Collections.Generic;
using UnityEngine;

// 아이템 조회표. 문자열 id → ItemDef.
// 씬·에셋 안에서는 ItemDef를 직접 참조하고, 이 조회표는 데이터 경계(맵 JSON·세이브)에서 쓴다.
[CreateAssetMenu(fileName = "ItemRegistry", menuName = "혹한/Item Registry")]
public class ItemRegistry : ScriptableObject
{
    public ItemDef[] items;

    private Dictionary<string, ItemDef> byId;

    private void OnEnable() => Rebuild();

    public void Rebuild()
    {
        byId = new Dictionary<string, ItemDef>();
        if (items == null) return;

        for (int i = 0; i < items.Length; i++)
        {
            ItemDef def = items[i];
            if (def == null) continue;

            if (string.IsNullOrWhiteSpace(def.id))
                Debug.LogWarning($"[아이템] {def.name}: id가 비어 있음 ('item/...' 형식으로 채울 것)");
            else if (byId.ContainsKey(def.id))
                Debug.LogWarning($"[아이템] id 중복: '{def.id}' ({def.name})");
            else
                byId[def.id] = def;
        }
    }

    // 정본 조회
    public ItemDef Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (byId == null) Rebuild();
        return byId.TryGetValue(id, out ItemDef def) ? def : null;
    }

    // 모든 항목의 id가 채워졌는지 확인 (M2 착수 조건)
    // 인스펙터에서 컴포넌트 우클릭 → "id 검사"로 실행
    [ContextMenu("id 검사")]
    public void ValidateIdsMenu() => ValidateIds();

    public bool ValidateIds(bool logDetails = true)
    {
        Rebuild();
        if (items == null) return false;

        int missing = 0;
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null) continue;
            if (!string.IsNullOrWhiteSpace(items[i].id)) continue;

            missing++;
            if (logDetails) Debug.LogWarning($"[아이템] id 미설정: {items[i].name}");
        }

        int count = items.Length;
        Debug.Log(missing == 0
            ? $"[아이템] id 검사 통과 — {count}종 모두 설정됨"
            : $"[아이템] id 미설정 {missing}/{count}종");

        return missing == 0;
    }
}