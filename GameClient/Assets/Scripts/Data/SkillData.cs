using UnityEngine;
using AutoBattler.Core;

namespace AutoBattler.Data
{
    [CreateAssetMenu(menuName = "AutoBattler/Skill", fileName = "Skill_")]
    public class SkillData : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;

        [Header("발동")]
        public float cooldown = 5f;
        public SkillTargetType targetType = SkillTargetType.SingleEnemy;
        public int range = 3;
        public int areaRadius = 1;          // AreaEnemy 일 때 사용

        [Header("효과")]
        public float damageMultiplier = 2f; // 공격력 * 배율
        public float healAmount = 0f;
        public float buffDuration = 0f;
        public Stats buff;                  // 자기/아군 강화량 (buffDuration 동안)

        [Header("합성")]
        public SkillData upgradedVersion;   // 동일 스킬북 3개 합성 결과 (다음 등급)

        // 실제 발동 로직은 SkillExecutor에서 targetType/필드 보고 분기.
        // 더 자유로운 효과는 ScriptableObject 상속/SkillEffect 컴포지션으로 확장.
    }
}
