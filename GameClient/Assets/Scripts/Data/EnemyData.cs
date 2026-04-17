using UnityEngine;

/// <summary>
/// 적 정적 데이터. 같은 종류의 적 프리팹은 이 에셋 하나를 공유.
/// 예: GoblinData.asset, OrcData.asset, ForestLordData.asset
/// Assets/Data/Enemies/ 폴더에 생성.
/// </summary>
[CreateAssetMenu(fileName = "EnemyData", menuName = "RPG/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("기본 정보")]
    public string enemyName;
    public EnemyType enemyType = EnemyType.Normal;
    public int level = 1;

    [Header("스탯")]
    public int maxHp = 30;
    public int attackDamage = 5;
    public int defense = 0;

    [Header("경험치 / 골드")]
    [Tooltip("GameFormulas.GetXPReward()에서 레벨/타입 보정 후 실제 지급량 계산.")]
    public int baseXP = 20;
    [Tooltip("처치 시 드롭되는 총 골드량. EnemyDropper에서 dropCount로 나눠 코인당 수량 계산.")]
    public int goldDrop = 10;
}
