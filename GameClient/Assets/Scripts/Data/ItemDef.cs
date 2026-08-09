using UnityEngine;

public enum ItemCategory { Resource, Tool, Food }

// 도구 종류. 같은 종류 안에서 위력이 높은 것이 자동으로 쓰인다(도구 사다리).
public enum ToolType { None = 0, Axe = 1, Pickaxe = 2, Gun = 3, Bow = 4 }

// 아이템 1종의 정의. 칸별 스택 상한 / 분류 / 음식 회복량 / 연료·도구·설치물 속성.
[CreateAssetMenu(fileName = "ItemDef", menuName = "혹한/Item Def")]
public class ItemDef : ScriptableObject
{
    public ResourceKind kind;
    public string displayName = "이름";
    public ItemCategory category = ItemCategory.Resource;

    [Header("칸")]
    [Tooltip("한 칸에 쌓이는 최대 수량. 도구는 1")]
    public int stackLimit = 99;

    [Header("도구 (category=Tool일 때)")]
    public ToolType toolType = ToolType.None;
    [Tooltip("1회 사용에 들어가는 타격량. 사다리 상위 도구일수록 크게")]
    public int hitPower = 1;
    [Tooltip("판정 방식(반경형·직선형 등). 없으면 공격해도 아무것도 맞지 않음")]
    public AttackPattern attackPattern;

    [Header("설치물 (건설 모드로 놓는 아이템)")]
    [Tooltip("설치할 프리팹. 비어 있으면 설치 불가 아이템")]
    public GameObject placementPrefab;
    [Tooltip("차지하는 칸 수. 프리팹의 GridOccupant가 있으면 그 값이 우선")]
    public Vector2Int placementFootprint = new Vector2Int(1, 1);

    [Header("원거리 (총·활)")]
    [Tooltip("1회 발사에 소모할 탄약 종류. None이면 소모 없음")]
    public ResourceKind ammoKind = ResourceKind.Ammo;
    [Tooltip("탄약을 소모할지 여부")]
    public bool consumesAmmo = false;

    [Header("연료")]
    [Tooltip("화로에 넣었을 때 1개당 연료량. 0이면 연료로 쓸 수 없음")]
    public float fuelValue = 0f;

    [Header("음식 (category=Food일 때만 의미 있음)")]
    [Tooltip("1개 섭취 시 허기 회복량. 음식이 아니면 0으로 둘 것")]
    public float hungerRestore = 0f;

    public bool IsFuel => fuelValue > 0f;
    // 핫바에 올릴 수 있는 것 = 장비(도끼·곡괭이·창·활·횃불)
    public bool IsEquipment => category == ItemCategory.Tool;
    public bool IsPlaceable => placementPrefab != null;
    public bool IsTool => category == ItemCategory.Tool;
    public bool IsFood => category == ItemCategory.Food;
}