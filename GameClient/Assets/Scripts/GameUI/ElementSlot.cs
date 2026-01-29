using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public class ElementSlot : MonoBehaviour
    {
        public Image icon;
        public Button button;
        public Image lockImage;

        public void SetIcon(Sprite sprite)
        {
            icon.sprite = sprite;
        }

        public void SetLockImage(bool isLock)
        {
            lockImage.gameObject.SetActive(isLock);
            Debug.Log(isLock);
        }
    }
}
