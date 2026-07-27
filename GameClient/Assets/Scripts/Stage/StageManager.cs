using UnityEngine;

/// <summary>
/// 스테이지 관리 (3D). 프로토타입 단계에서는 XZ 평면 격자(오와 열) 배치를 코드로 생성.
/// 이후 ScriptableObject/JSON 스테이지 데이터 + 커스텀 에디터 툴로 교체 예정.
/// </summary>
public class StageManager : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] Enemy enemyPrefab;

    [Header("격자 배치 (프로토타입)")]
    [SerializeField] int columns = 5;
    [SerializeField] int rows = 3;
    [SerializeField] float spacingX = 1.1f;   // 투사체가 사이로 빠지는 재미를 위한 간격
    [SerializeField] float spacingZ = 1.1f;
    [Tooltip("격자 맨 앞줄 중앙의 위치. y는 바닥 높이(모델 발 위치).")]
    [SerializeField] Vector3 gridFrontCenter = new Vector3(0f, 0f, 2f);
    [SerializeField] int enemyHp = 30;

    public int RemainingEnemies { get; private set; }

    void Start()
    {
        SpawnGrid();
    }

    void SpawnGrid()
    {
        float startX = gridFrontCenter.x - (columns - 1) * spacingX * 0.5f;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                Vector3 pos = new Vector3(
                    startX + c * spacingX,
                    gridFrontCenter.y,
                    gridFrontCenter.z + r * spacingZ); // 뒷줄일수록 +z (화면 위쪽)

                var enemy = Instantiate(enemyPrefab, pos, Quaternion.identity, transform);
                enemy.Init(enemyHp);
                RemainingEnemies++;
            }
        }
    }

    /// <summary>BattleManager가 EnemyKilled 이벤트를 받아 호출.</summary>
    public void NotifyEnemyKilled(Enemy e)
    {
        RemainingEnemies = Mathf.Max(0, RemainingEnemies - 1);
    }
}
