using UnityEngine;

// 작업막사 공통 구조를 데이터로 분화. 코드/모델 1벌 + 이 Def로 벌목막사/채집오두막 구분.
[CreateAssetMenu(fileName = "WorkBuildingDef", menuName = "혹한/Work Building Def")]
public class WorkBuildingDef : ScriptableObject
{
    [Header("표시")]
    public string buildingName;    // 예: 벌목막사, 채집오두막
    public GameObject propPrefab;   // 소품(임시 아트) — 공통 구조에 끼움

    [Header("생산")]
    public ResourceKind produces;   // 벌목막사=Firewood, 채집오두막=Food
    public int amountPerCycle = 1;
    public float cycleSeconds = 3f; // 1회 생산 주기

    [Header("슬롯")]
    public int slotCount = 1;       // 배정 가능 인원
}
