using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public class GuageBar : MonoBehaviour
    {
        public Image fill;
        public TMP_Text amountText;
        public RectTransform rectTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        public void SetGuage(long fullAmount, long nowAmount)
        {
            if (fullAmount == 0)
                return;

            if (nowAmount < 0)
                nowAmount = 0;

            fill.fillAmount = (float)nowAmount / fullAmount;
            amountText.text = $"{nowAmount}";
        }

        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }
    }
}
