using UnityEngine;

namespace GameUI
{
    public class UIManager : MonoBehaviour
    {
        public HudLayout hudLayout;
        public GameLayout gameLayout;

        private void Start()
        {
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.OnHpChanged += gameLayout.SetHpGuage;
                PlayerDataManager.Instance.OnMpChanged += gameLayout.SetMpGuage;
                PlayerDataManager.Instance.OnMoneyChanged += gameLayout.SetMoneyText;
                PlayerDataManager.Instance.OnExpChanged += gameLayout.SetExpGuage;
                PlayerDataManager.Instance.OnLevelChanged += gameLayout.SetLevelText;

                PlayerDataManager.Instance.InitializeUI();
            }
        }

        private void OnDestroy()
        {
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.OnHpChanged -= gameLayout.SetHpGuage;
                PlayerDataManager.Instance.OnMpChanged -= gameLayout.SetMpGuage;
                PlayerDataManager.Instance.OnMoneyChanged -= gameLayout.SetMoneyText;
                PlayerDataManager.Instance.OnExpChanged -= gameLayout.SetExpGuage;
                PlayerDataManager.Instance.OnLevelChanged -= gameLayout.SetLevelText;
            }
        }
    }
}
