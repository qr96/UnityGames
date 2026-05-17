using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AutoBattler.Heroes;
using AutoBattler.Rounds;

namespace AutoBattler.UI
{
    /// <summary>
    /// LoadoutScreen의 자식. 영웅 한 명의 카드.
    /// 슬롯 버튼을 누르면:
    ///   - 인벤토리에서 선택된 스킬이 있으면 → 그 스킬을 슬롯에 장착
    ///   - 선택된 스킬이 없으면 → 슬롯의 스킬을 인벤토리로 해제
    /// </summary>
    public class HeroLoadoutCard : MonoBehaviour
    {
        [Header("표시")]
        public TMP_Text nameText;
        public TMP_Text slotALabel;
        public TMP_Text slotBLabel;

        [Header("버튼")]
        public Button slotAButton;
        public Button slotBButton;

        private LoadoutScreen _owner;
        private Hero _hero;

        public void Bind(LoadoutScreen owner, Hero hero)
        {
            _owner = owner;
            _hero  = hero;

            if (slotAButton != null)
            {
                slotAButton.onClick.RemoveAllListeners();
                slotAButton.onClick.AddListener(() => OnSlotClicked(RunManager.LoadoutSlot.A));
            }
            if (slotBButton != null)
            {
                slotBButton.onClick.RemoveAllListeners();
                slotBButton.onClick.AddListener(() => OnSlotClicked(RunManager.LoadoutSlot.B));
            }

            Refresh();
        }

        public void Refresh()
        {
            if (_hero == null) return;
            if (nameText  != null) nameText.text  = _hero.data != null ? _hero.data.displayName : "?";
            if (slotALabel != null) slotALabel.text = _hero.skillA != null ? _hero.skillA.displayName : "(빈 슬롯)";
            if (slotBLabel != null) slotBLabel.text = _hero.skillB != null ? _hero.skillB.displayName : "(빈 슬롯)";
        }

        private void OnSlotClicked(RunManager.LoadoutSlot slot)
        {
            if (_owner == null || _hero == null) return;

            if (_owner.SelectedSkill != null)
            {
                // 선택된 스킬 장착
                _owner.EquipSelectedTo(_hero, slot);
            }
            else
            {
                // 선택 없음 → 해당 슬롯 해제
                _owner.UnequipFrom(_hero, slot);
            }
            Refresh();
        }
    }
}
