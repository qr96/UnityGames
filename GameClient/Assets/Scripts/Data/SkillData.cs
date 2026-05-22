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

        [Header("투사체 (선택)")]
        [Tooltip("지정 시 발사 후 도착 시점에 효과 발동. null이면 즉발.")]
        public GameObject projectilePrefab;
        [Tooltip("월드 m/s. 8이면 8m/s.")]
        public float projectileSpeed = 8f;

        // 실제 발동 로직은 SkillExecutor에서 targetType/필드 보고 분기.
        // 더 자유로운 효과는 ScriptableObject 상속/SkillEffect 컴포지션으로 확장.

        // ─────────────────────────────────────────────────────────
        // 표시용 요약
        // ─────────────────────────────────────────────────────────

        /// <summary>효과 한 줄 요약. 예: "단일 250% 피해", "광역 200% 피해(반경 1)", "회복 30"</summary>
        public string GetEffectSummary()
        {
            switch (targetType)
            {
                case SkillTargetType.SingleEnemy:
                    return damageMultiplier > 0f
                        ? $"단일 {Mathf.RoundToInt(damageMultiplier * 100)}% 피해"
                        : "효과 없음";

                case SkillTargetType.AreaEnemy:
                    return damageMultiplier > 0f
                        ? $"광역 {Mathf.RoundToInt(damageMultiplier * 100)}% 피해 (반경 {areaRadius})"
                        : $"광역 효과 (반경 {areaRadius})";

                case SkillTargetType.Self:
                    if (healAmount > 0f) return $"자가 회복 {Mathf.RoundToInt(healAmount)}";
                    return "자가 효과";

                case SkillTargetType.AllyLowestHP:
                    if (healAmount > 0f) return $"아군 회복 {Mathf.RoundToInt(healAmount)} (체력 낮은 아군)";
                    return "아군 효과";
            }
            return "";
        }

        /// <summary>대상 유형 요약. 예: "적", "광역", "자신", "아군"</summary>
        public string GetTargetSummary()
        {
            switch (targetType)
            {
                case SkillTargetType.SingleEnemy: return "적";
                case SkillTargetType.AreaEnemy: return "광역";
                case SkillTargetType.Self: return "자신";
                case SkillTargetType.AllyLowestHP: return "아군";
            }
            return "";
        }
    }
}