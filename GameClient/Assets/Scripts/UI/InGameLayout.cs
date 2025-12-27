using InGame;
using TMPro;
using UnityEngine;

namespace GameUI
{
    public class InGameLayout : MonoBehaviour
    {
        public TMP_Text foodText;
        public GameObject holdIcon;
        public GameObject freeIcon;

        private void Start()
        {
            InGamePropertyManager.Instance.OnChangeFoodEvent += OnChangeFood;
            PlayerMover.Instance.OnChangeHoldMode += OnChangeHoldMode;
        }

        private void OnDestroy()
        {
            InGamePropertyManager.Instance.OnChangeFoodEvent -= OnChangeFood;
            PlayerMover.Instance.OnChangeHoldMode -= OnChangeHoldMode;
        }

        void OnChangeFood(long food)
        {
            foodText.text = food.ToString();
        }

        void OnChangeHoldMode(bool holdMode)
        {
            holdIcon.SetActive(holdMode);
            freeIcon.SetActive(!holdMode);
        }
    }
}
