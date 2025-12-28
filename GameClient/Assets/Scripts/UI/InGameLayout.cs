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
            // 로딩 끝나면 호출되도록 수정 예정
            if (FieldManager.Instance.TryGetProperty(1, out var property))
                property.OnChangeFoodEvent += OnChangeFood;
            PlayerMover.Instance.OnChangeHoldMode += OnChangeHoldMode;
        }

        private void OnDestroy()
        {
            if (FieldManager.Instance.TryGetProperty(1, out var property))
                property.OnChangeFoodEvent -= OnChangeFood;
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
