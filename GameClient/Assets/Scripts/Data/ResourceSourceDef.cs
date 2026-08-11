using System;
using UnityEngine;

// 자원원 밸런스 정의. 종류별로 하나씩 만든다(나무·베리 덤불·잔가지 등).
// 인스턴스(배치된 개체)는 ResourceSource가 담당하고, 밸런스 값은 전부 여기 있다.
// 맵 파일은 이 Def의 id와 위치만 지정한다.
[CreateAssetMenu(fileName = "ResourceSourceDef", menuName = "혹한/Resource Source Def")]
public class ResourceSourceDef : ScriptableObject
{
    [Serializable]
    public struct Yield
    {
        public ItemDef item;
        public int amount;
    }

    public enum YieldMode
    {
        Instant,  // 즉시 인벤토리로 (손 채집)
        Drop,     // 바닥에 떨어뜨림 (벌목·채석)
    }

    [Header("식별자")]
    [Tooltip("문자열 id — 'resource/tree_pine' 형식. 맵 파일이 이 값을 가리킨다")]
    public string id;
    public string displayName = "자원";
    [Tooltip("상호작용·타격 라벨에 쓰는 동작 이름")]
    public string prompt = "채집";

    [Header("수확 방식")]
    [Tooltip("None이면 맨손 채집(E 한 번에 즉시). 그 외는 그 도구를 들고 스윙해야 한다")]
    public ToolType requiredTool = ToolType.None;
    [Tooltip("도구 수확일 때의 내구도. 스윙 1회에 도구 위력(hitPower)만큼 깎인다")]
    public int health = 3;

    [Header("산출")]
    public Yield[] yields;
    public YieldMode yieldMode = YieldMode.Instant;
    [Tooltip("소진되기까지 수확할 수 있는 횟수")]
    public int charges = 1;

    [Header("드랍 (yieldMode = Drop)")]
    [Tooltip("DroppedItem이 붙은 프리팹")]
    public GameObject dropPrefab;
    [Tooltip("한 덩이당 수량")]
    public int amountPerDrop = 1;
    [Tooltip("주변에 흩어지는 반경")]
    public float scatterRadius = 1.2f;

    [Header("재생")]
    [Tooltip("소진 후 재생까지 시간(초). 0 이하면 소진 시 제거된다")]
    public float regenSeconds = 0f;

    [Header("월드")]
    [Tooltip("격자 칸을 점유할지 — 그 칸에 건물을 못 짓게 된다")]
    public bool occupiesCell = false;

    public bool IsHandGathered => requiredTool == ToolType.None;
    public int Health => Mathf.Max(1, health);
    public int Charges => Mathf.Max(1, charges);
}
