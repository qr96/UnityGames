using UnityEngine;

/// <summary>
/// 장비 원본 데이터. 같은 종류의 장비는 이 에셋 하나를 공유.
/// Assets/Data/Equipments/ 폴더에 생성.
/// </summary>
[CreateAssetMenu(fileName = "EquipmentData", menuName = "RPG/Equipment Data")]
public class EquipmentData : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("저장/불러오기 시 장비를 식별하는 고유 ID. 절대 중복 불가.")]
    public string id;
    public string equipmentName;
    public EquipmentSlot slot;
    public EquipmentTier tier;
    public Sprite icon;
    public int buyPrice;

    [Header("기본 스탯")]
    public StatType baseStat;
    public float baseStatValue;

    [Header("강화")]
    public int maxEnhanceLevel;

    // ── 계산 ──────────────────────────────────────────────────────────────

    /// <summary>강화 수치 포함 기본 스탯값.</summary>
    public float GetBaseStatValue(int enhanceLevel)
    {
        return baseStatValue + baseStatValue * 0.1f * enhanceLevel;
    }

    /// <summary>티어 = 랜덤 옵션 개수 (티어1→1개 ~ 티어4→4개).</summary>
    public int GetOptionCount() => (int)tier;
}