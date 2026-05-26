using UnityEngine;
using AutoBattler.Core;

namespace AutoBattler.Data
{
    // ─────────────────────────────────────────────────────────
    // 타깃팅 3축
    // ─────────────────────────────────────────────────────────

    /// <summary>스킬이 누구를 대상으로 하는지.</summary>
    public enum SkillTeamFilter
    {
        Enemy,   // 적
        Ally,    // 아군 (시전자 포함 가능 — selector가 결정)
        Self,    // 시전자 자신 (range/selector 무시)
    }

    /// <summary>대상 후보의 거리 제한.</summary>
    public enum SkillRangeFilter
    {
        WithinSkillRange, // SkillData.range 안의 후보만
        Global,           // 거리 무관, 전 그리드
    }

    /// <summary>후보 중 한 명을 고르는 규칙.</summary>
    public enum SkillTargetSelector
    {
        Nearest,   // 시전자에서 가장 가까운 유닛
        LowestHP,  // 현재 HP가 가장 낮은 유닛
        Self,      // 시전자 자신 (TeamFilter.Self 와 함께 사용)
    }

    [CreateAssetMenu(menuName = "AutoBattler/Skill", fileName = "Skill_")]
    public class SkillData : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;

        [Header("발동")]
        public float cooldown = 5f;
        [Tooltip("스킬 사거리(칸). rangeFilter=WithinSkillRange일 때만 의미 있음.")]
        public int range = 3;

        [Header("타깃팅 (3축 조합)")]
        public SkillTeamFilter teamFilter = SkillTeamFilter.Enemy;
        public SkillRangeFilter rangeFilter = SkillRangeFilter.WithinSkillRange;
        public SkillTargetSelector selector = SkillTargetSelector.Nearest;

        [Header("효과")]
        [Tooltip("공격력 * 배율. 0이면 데미지 없음.")]
        public float damageMultiplier = 2f;
        [Tooltip("회복량. 0이면 회복 없음. 회복 스킬은 부상 대상이 없으면 발동하지 않음.")]
        public float healAmount = 0f;

        [Header("광역")]
        [Tooltip("타깃 주변 N칸의 같은 진영 유닛에게도 효과 (8방향, 체비셰프). 0이면 단일.")]
        public int splashRadius = 0;

        [Header("버프 (미사용 — 향후 확장)")]
        public float buffDuration = 0f;
        public Stats buff;

        [Header("합성")]
        public SkillData upgradedVersion;   // 동일 스킬북 3개 합성 결과 (다음 등급)

        [Header("투사체 (선택)")]
        [Tooltip("지정 시 발사 후 도착 시점에 효과 발동. null이면 즉발.")]
        public GameObject projectilePrefab;
        [Tooltip("월드 m/s. 8이면 8m/s.")]
        public float projectileSpeed = 8f;

        // ─────────────────────────────────────────────────────────
        // 표시용 요약
        // ─────────────────────────────────────────────────────────

        /// <summary>효과 한 줄 요약. 예: "250% 피해", "회복 30", "광역 200% 피해 (반경 1)"</summary>
        public string GetEffectSummary()
        {
            string splash = splashRadius > 0 ? $"광역 (반경 {splashRadius}) " : "";
            if (healAmount > 0f)
                return $"{splash}회복 {Mathf.RoundToInt(healAmount)}";
            if (damageMultiplier > 0f)
                return $"{splash}{Mathf.RoundToInt(damageMultiplier * 100)}% 피해";
            return "효과 없음";
        }

        /// <summary>대상 유형 요약. 예: "적 (가장 가까운)", "아군 (체력 낮은)", "자신"</summary>
        public string GetTargetSummary()
        {
            if (teamFilter == SkillTeamFilter.Self) return "자신";

            string team = teamFilter == SkillTeamFilter.Enemy ? "적" : "아군";
            string sel = selector switch
            {
                SkillTargetSelector.Nearest => "가장 가까운",
                SkillTargetSelector.LowestHP => "체력 낮은",
                _ => "",
            };
            string scope = rangeFilter == SkillRangeFilter.Global ? " · 거리무관" : "";
            return string.IsNullOrEmpty(sel) ? team + scope : $"{team} ({sel}){scope}";
        }
    }
}