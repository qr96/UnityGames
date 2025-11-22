using UnityEngine;

namespace GameUI
{
    public class UIManager : MonoBehaviour
    {
        public HudLayout hudLayout;
        public GameLayout gameLayout;

        private void OnEnable()
        {
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.OnHpChanged += gameLayout.SetHpGuage;
                PlayerDataManager.Instance.OnMoneyChanged += gameLayout.SetMoneyText;
                PlayerDataManager.Instance.OnExpChanged += gameLayout.SetExpGuage;
                PlayerDataManager.Instance.OnLevelChanged += gameLayout.SetLevelText;
            }
        }

        private void OnDisable()
        {
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.OnHpChanged -= gameLayout.SetHpGuage;
                PlayerDataManager.Instance.OnMoneyChanged -= gameLayout.SetMoneyText;
                PlayerDataManager.Instance.OnExpChanged -= gameLayout.SetExpGuage;
                PlayerDataManager.Instance.OnLevelChanged -= gameLayout.SetLevelText;
            }
        }

        private void Start()
        {
            PlayerDataManager.Instance.InitializeUI();
        }
    }
}
