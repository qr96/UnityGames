using InGame;
using TMPro;
using UnityEngine;

namespace GameUI
{
    public class InGameLayout : MonoBehaviour
    {
        public TMP_Text foodText;

        private void Start()
        {
            InGamePropertyManager.Instance.OnChangeFoodEvent += OnChangeFood;
        }

        private void OnDestroy()
        {
            InGamePropertyManager.Instance.OnChangeFoodEvent -= OnChangeFood;
        }

        void OnChangeFood(long food)
        {
            foodText.text = food.ToString();
        }
    }
}
