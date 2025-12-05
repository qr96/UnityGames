using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public class EquipmentSlot : MonoBehaviour
    {
        public Button button;
        public TMP_Text level;
        public Image image;

        public Action OnClick;

        private void Start()
        {
            button.onClick.AddListener(() => OnClick?.Invoke());
        }

        public void SetLevel(int level)
        {
            this.level.text = $"+{level}";
        }

        public void SetImage(Sprite sprite)
        {
            image.sprite = sprite;
        }
    }
}
