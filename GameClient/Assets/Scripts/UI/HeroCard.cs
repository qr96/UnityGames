using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AutoBattler.Heroes;
using AutoBattler.Rounds;

namespace AutoBattler.UI
{
    /// <summary>
    /// 영웅 카드 (1명). 하단 패널의 자식으로 5개가 가로로 배치됨.
    ///
    /// 표시:
    ///   - 영웅 이름
    ///   - 현재 HP / 최대 HP (전투 중엔 Hero.OnHPChanged 이벤트로 갱신)
    ///   - 스킬 A/B 슬롯 (이름)
    ///
    /// 상호작용:
    ///   - 슬롯 탭 → HeroCardPanel을 통해 RunManager에 장착 시도
    ///   - 전투 중이면 RunManager가 거부
    ///
    /// 설계 노트:
    ///   카드는 Hero(영구 모델)만 본다. BattleUnit(전투 런타임)은 직접 추적하지 않음.
    ///   BattleUnit은 데미지 발생 시 Hero.SetCurrentHP로 모델에 반영하고,
    ///   Hero가 발사하는 OnHPChanged 이벤트를 카드가 받아 갱신한다.
    ///   → BattleUnit GameObject의 풀링/재스폰 라이프사이클과 카드 UI가 완전히 분리됨.
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

        // ─────────────────────────────────────────────────────────
        public void Bind(HeroCardPanel owner, Hero hero)
        {
            // 이전 영웅 구독 해제
            if (_hero != null) _hero.OnHPChanged -= Refresh;

            _owner = owner;
            _hero = hero;

            if (_hero != null) _hero.OnHPChanged += Refresh;

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

        private void OnDestroy()
        {
            if (_hero != null) _hero.OnHPChanged -= Refresh;
        }

        /// <summary>영웅 정보(이름/스킬/HP) 갱신. Hero.OnHPChanged에서도 호출됨.</summary>
        public void Refresh()
        {
            if (_hero == null) return;

            if (nameText != null)
                nameText.text = _hero.data != null ? _hero.data.displayName : "?";
            if (slotALabel != null)
                slotALabel.text = _hero.skillA != null ? _hero.skillA.displayName : "(빈)";
            if (slotBLabel != null)
                slotBLabel.text = _hero.skillB != null ? _hero.skillB.displayName : "(빈)";

            float maxHp = _hero.GetFinalStats().maxHp;
            SetHp(_hero.CurrentHP, maxHp);
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