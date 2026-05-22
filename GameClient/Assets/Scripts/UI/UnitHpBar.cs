using UnityEngine;
using UnityEngine.UI;

namespace AutoBattler.Battle.FX
{
    /// <summary>
    /// 유닛 머리 위 HP바. 공용 World Space Canvas 자식.
    /// UnitHpBarManager가 풀링/추적 담당.
    /// </summary>
    public class UnitHpBar : MonoBehaviour
    {
        [Header("UI")]
        public Slider slider;
        public Image fillImage;

        [Header("색")]
        public Color allyColor = new Color(0.2f, 0.85f, 0.2f);   // 진한 초록
        public Color enemyColor = new Color(0.95f, 0.15f, 0.15f); // 진한 빨강
        public Color lowHpColor = new Color(1f, 0.6f, 0.1f);      // 주황

        [Header("동작")]
        public bool hideWhenFull = false;
        public bool useLowHpColor = true;

        [Header("위치")]
        public float yOffset = 1.5f;

        // 상태
        private Color _baseColor;
        private BattleUnit _target;

        // ─────────────────────────────────────────────────────────
        public void Bind(BattleUnit target)
        {
            _target = target;

            bool isAlly = target.Team == AutoBattler.Core.Team.Ally;
            _baseColor = isAlly ? allyColor : enemyColor;
            if (fillImage != null) fillImage.color = _baseColor;

            if (slider != null) slider.value = 1f;
            gameObject.SetActive(true);
        }

        public void Unbind()
        {
            _target = null;
            gameObject.SetActive(false);
        }

        public BattleUnit Target => _target;

        // ─────────────────────────────────────────────────────────
        /// <summary>매니저가 매 LateUpdate에 호출.</summary>
        public void FollowTarget(Camera cam)
        {
            if (_target == null) return;
            if (!_target.IsAlive)
            {
                gameObject.SetActive(false);
                return;
            }

            // 위치: 타깃 머리 위
            transform.position = _target.transform.position + Vector3.up * yOffset;

            // 빌보드: 카메라 회전을 그대로 사용 (각 HP바가 일관된 각도. Top-down에서 자연스러움)
            if (cam != null)
                transform.rotation = cam.transform.rotation;

            // HP 갱신
            UpdateHp();
        }

        private void UpdateHp()
        {
            float cur = _target.CurrentHP;
            float max = _target.Stats.maxHp;
            if (max <= 0f) { gameObject.SetActive(false); return; }

            float ratio = Mathf.Clamp01(cur / max);
            if (slider != null) slider.value = ratio;

            if (fillImage != null && useLowHpColor)
                fillImage.color = ratio <= 0.3f ? lowHpColor : _baseColor;

            bool full = ratio >= 1f && hideWhenFull;
            bool show = cur > 0f && !full;
            if (gameObject.activeSelf != show) gameObject.SetActive(show);
        }
    }
}