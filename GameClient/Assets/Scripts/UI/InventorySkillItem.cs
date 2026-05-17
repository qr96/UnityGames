using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AutoBattler.Data;

namespace AutoBattler.UI
{
    /// <summary>
    /// LoadoutScreen 인벤토리의 한 행.
    /// 본체(selectButton) 탭 → LoadoutScreen.SelectSkill 호출
    /// 합성 가능(3개+)이면 fuseButton 활성, 누르면 합성 실행
    /// </summary>
    public class InventorySkillItem : MonoBehaviour
    {
        [Header("표시")]
        public TMP_Text labelText;          // "Fireball x3" 형식
        public Image    selectedHighlight;  // 선택 상태 표시 (옵션)

        [Header("버튼")]
        public Button selectButton;         // 본체 (선택)
        public Button fuseButton;           // 합성

        private LoadoutScreen _owner;
        private SkillData     _skill;
        private int           _count;

        public void Bind(LoadoutScreen owner, SkillData skill, int count)
        {
            _owner = owner;
            _skill = skill;
            _count = count;

            if (labelText != null)
                labelText.text = $"{skill.displayName} x{count}";

            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(OnSelectClicked);
            }

            if (fuseButton != null)
            {
                fuseButton.onClick.RemoveAllListeners();
                fuseButton.onClick.AddListener(OnFuseClicked);
                bool canFuse = count >= 3 && skill.upgradedVersion != null;
                fuseButton.gameObject.SetActive(canFuse);
            }

            UpdateSelectedHighlight();
        }

        public void UpdateSelectedHighlight()
        {
            if (selectedHighlight == null || _owner == null) return;
            selectedHighlight.enabled = (_owner.SelectedSkill == _skill);
        }

        private void OnSelectClicked()
        {
            if (_owner == null) return;
            // 이미 선택된 거 다시 탭하면 해제
            if (_owner.SelectedSkill == _skill) _owner.SelectSkill(null);
            else                                _owner.SelectSkill(_skill);
        }

        private void OnFuseClicked()
        {
            _owner?.FuseSkill(_skill);
        }
    }
}
