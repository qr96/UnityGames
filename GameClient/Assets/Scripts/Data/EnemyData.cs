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

    [Header("경험치")]
    [Tooltip("GameFormulas.GetXPReward()에서 레벨/타입 보정 후 실제 지급량 계산.")]
    public int baseXP = 20;
}
