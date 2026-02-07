using UnityEngine;
using UnityEngine.UI;

public class ScrollSlot : MonoBehaviour
{
    public Button button;
    public Image frame;

    public void UseScroll(bool used)
    {
        var prevColor = frame.color;
        prevColor.a = used ? 0.5f : 1f;
        frame.color = prevColor;
    }

    public void SelectScroll(bool selected)
    {
        frame.rectTransform.localScale = selected ? Vector3.one * 1.2f : Vector3.one;
    }
}
