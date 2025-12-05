using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public class PlayerInfoPopup : MonoBehaviour
    {
        public EquipmentSlot weapon;
        public EquipmentSlot hat;
        public EquipmentSlot glove;
        public EquipmentSlot belt;
        public EquipmentSlot shoe;
        public EquipmentSlot ring;

        public TMP_Text info;

        public Button closeButton;

        private void Start()
        {
            weapon.OnClick += () => PlayerDataManager.Instance.AddWeaponSlotLevel();
            closeButton.onClick.AddListener(() => Hide());
        }

        public void Show()
        {
            PlayerDataManager.Instance.OnWeaponSlotChange += SetWeaponSlot;

            SetWeaponSlot(PlayerDataManager.Instance.Data.weaponSlot);

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            PlayerDataManager.Instance.OnWeaponSlotChange -= SetWeaponSlot;
            gameObject.SetActive(false);
        }

        public void SetWeaponSlot(int level)
        {
            weapon.SetLevel(level);
        }
    }
}
