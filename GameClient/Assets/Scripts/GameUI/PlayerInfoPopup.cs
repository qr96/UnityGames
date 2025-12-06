using GameData;
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

        public TMP_Text statInfo;

        public Button closeButton;

        private void Start()
        {
            weapon.OnClick += () => PlayerDataManager.Instance.AddWeaponSlotLevel();
            closeButton.onClick.AddListener(() => Hide());
        }

        public void Show()
        {
            PlayerDataManager.Instance.OnWeaponSlotChange += SetWeaponSlot;
            PlayerDataManager.Instance.OnStatChanged += SetStatInfo;
            SetWeaponSlot(PlayerDataManager.Instance.Data.weaponSlot);
            SetStatInfo(PlayerDataManager.Instance.Model.MaxStat);

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            PlayerDataManager.Instance.OnWeaponSlotChange -= SetWeaponSlot;
            PlayerDataManager.Instance.OnStatChanged -= SetStatInfo;

            gameObject.SetActive(false);
        }

        public void SetWeaponSlot(int level)
        {
            weapon.SetLevel(level);
        }

        public void SetStatInfo(Stat stat)
        {
            var statText = $"HP : {stat.hp}\n";
            statText += $"ATK : {stat.attack}\n";
            statText += $"DEF: {stat.defense}\n";
            statText += $"CRI : {0}%\n";
            statText += $"CRIDAM : {0}%";

            statInfo.text = statText;
        }
    }
}
