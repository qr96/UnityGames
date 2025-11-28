using TMPro;
using UnityEngine;

namespace GameUI
{
    public class GameLayout : MonoBehaviour
    {
        public TMP_Text levelText;
        public TMP_Text moneyText;
        public GuageBar hpGuage;
        public GuageBar mpGuage;
        public GuageBar expGuage;
        
        public void SetLevelText(long level, long prevLevel)
        {
            levelText.text = $"Lv. {level}";
        }

        public void SetMoneyText(long money)
        {
            moneyText.text = $"{money}";
        }

        public void SetHpGuage(long max, long now)
        {
            hpGuage.SetGuage(max, now);
        }

        public void SetMpGuage(long max, long now)
        {
            mpGuage.SetGuage(max, now);
        }

        public void SetExpGuage(long max, long now)
        {
            expGuage.SetGuage(max, now);
        }
    }
}
