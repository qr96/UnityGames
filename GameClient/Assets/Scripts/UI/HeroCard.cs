using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AutoBattler.Battle;
using AutoBattler.Heroes;
using AutoBattler.Rounds;

namespace AutoBattler.UI
{
    /// <summary>
    /// 영웅 카드 (1명). 하단 패널의 자식으로 5개가 가로로 배치됨.
    ///
    /// 표시:
    ///   - 영웅 이름
    ///   - 현재 HP / 최대 HP (전투 중엔 실시간)
    ///   - 스킬 A/B 슬롯 (이름)
    ///
    /// 상호작용:
    ///   - 슬롯 탭 → HeroCardPanel을 통해 RunManager에 장착 시도
    ///   - 전투 중이면 RunManager가 거부
    /// </summary>
    public class HeroCard : MonoBehaviour
    {
        [Header("표시")]
        public TMP_Text nameText;
        public Slider hpSlider;
        public TMP_Text hpText;
        public TMP_Text slotALabel;
        public TMP_Text slotBLabel;

        [Header("상호작용")]
        public Button slotAButton;
        public Button slotBButton;

        // 데이터
        private HeroCardPanel _owner;
        private Hero _hero;

        // 전투 중 추적할 BattleUnit (HP 갱신용)
        private BattleUnit _trackedUnit;

        // ─────────────────────────────────────────────────────────
        public void Bind(HeroCardPanel owner, Hero hero)
        {
            _owner = owner;
            _hero = hero;

            if (slotAButton != null)
            {
                slotAButton.onClick.RemoveAllListeners();
                slotAButton.onClick.AddListener(() => _owner.OnSlotClicked(_hero, RunManager.LoadoutSlot.A));
            }
            if (slotBButton != null)
            {
                slotBButton.onClick.RemoveAllListeners();
                slotBButton.onClick.AddListener(() => _owner.OnSlotClicked(_hero, RunManager.LoadoutSlot.B));
            }

            Refresh();
        }

        /// <summary>외부에서 호출 — 영웅 정보(이름/스킬) 갱신.</summary>
        public void Refresh()
        {
            if (_hero == null) return;
            if (nameText != null)
                nameText.text = _hero.data != null ? _hero.data.displayName : "?";
            if (slotALabel != null)
                slotALabel.text = _hero.skillA != null ? _hero.skillA.displayName : "(빈)";
            if (slotBLabel != null)
                slotBLabel.text = _hero.skillB != null ? _hero.skillB.displayName : "(빈)";

            // 전투 외부에선 만피로 표시
            var finalStats = _hero.GetFinalStats();
            SetHp(finalStats.maxHp, finalStats.maxHp);
        }

        /// <summary>전투 시작 시 호출 — 이 카드가 어떤 BattleUnit을 추적할지 지정.</summary>
        public void TrackUnit(BattleUnit unit)
        {
            _trackedUnit = unit;
        }

        /// <summary>전투 종료 시 호출.</summary>
        public void Untrack()
        {
            _trackedUnit = null;
        }

        // ─────────────────────────────────────────────────────────
        private void Update()
        {
            if (_trackedUnit == null) return;

            if (_trackedUnit.IsAlive)
            {
                SetHp(_trackedUnit.CurrentHP, _trackedUnit.Stats.maxHp);
            }
            else
            {
                SetHp(0f, _trackedUnit.Stats.maxHp);
            }
        }

        private void SetHp(float cur, float max)
        {
            if (hpSlider != null)
                hpSlider.value = max > 0 ? cur / max : 0;
            if (hpText != null)
                hpText.text = $"{Mathf.CeilToInt(cur)}";
        }
    }
}