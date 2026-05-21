using UnityEngine;
using AutoBattler.Core;

namespace AutoBattler.Data
{
    /// <summary>
    /// 직업 SO. 영웅이 가진 직업이 무기 외형/모션/추가 수치를 결정.
    ///
    /// 예시:
    ///   Job_Novice  : 휘두르기 모션, 방망이 외형, 기본 수치
    ///   Job_Warrior : 휘두르기 모션, 검 외형, +공격력 +HP
    ///   Job_Archer  : 활쏘기 모션, 활 외형, +사거리 +공속
    /// </summary>
    [CreateAssetMenu(menuName = "AutoBattler/Job", fileName = "Job_")]
    public class JobData : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;

        [Header("수치 (영웅 baseStats에 합산)")]
        public Stats statBonus;

        [Header("기본 공격")]
        public AttackMotion attackMotion = AttackMotion.Swing;

        [Header("시각 (현재는 비워둬도 됨)")]
        [Tooltip("손에 부착될 무기 모델 프리팹. 모델 들어오면 사용.")]
        public GameObject weaponVisualPrefab;
        [Tooltip("기본 공격 이펙트 프리팹. 향후 풀링 키로 대체될 수도 있음.")]
        public GameObject hitVfxPrefab;
    }
}
