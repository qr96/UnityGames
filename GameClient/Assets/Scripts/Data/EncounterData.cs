using System;
using System.Collections.Generic;
using UnityEngine;

namespace AutoBattler.Data
{
    /// <summary>한 전투의 적 구성. 어떤 적이 어느 칸에 배치되는지의 목록.</summary>
    [CreateAssetMenu(menuName = "AutoBattler/Encounter", fileName = "Encounter_")]
    public class EncounterData : ScriptableObject
    {
        public string id;
        public string displayName;

        [Tooltip("이 인카운터에 등장하는 적들과 배치 위치")]
        public List<EncounterEnemy> enemies = new List<EncounterEnemy>();

        [Header("난이도 스케일 적용 여부")]
        [Tooltip("true면 라운드 진행도에 따라 적의 스탯이 스케일됨. false면 데이터 그대로.")]
        public bool applyDifficultyScaling = true;

        [Header("스탯 배율 (디자이너 수동 조절용)")]
        [Tooltip("이 인카운터만 더 강하게/약하게 하려면 사용. 1.0 = 그대로")]
        public float hpMultiplier     = 1f;
        public float attackMultiplier = 1f;
    }

    [Serializable]
    public class EncounterEnemy
    {
        public EnemyData enemyData;
        public Vector2Int cell;  // 그리드 셀 (적군 영역: y=4..9 권장)
    }
}
