using UnityEngine;
using UnityEngine.UI;

// uGUI 요소를 코드로 만들기 위한 도우미. 씬에 프리팹을 만들지 않아도 되게 한다.
public static class UIKit
{
    private static Font cachedFont;

    public static Font DefaultFont
    {
        get
        {
            if (cachedFont != null) return cachedFont;
            cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (cachedFont == null) cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return cachedFont;
        }
    }

    public static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    public static Image CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;   // 입력을 쓰지 않으므로 꺼서 비용 절약
        return img;
    }

    public static Text CreateText(string name, Transform parent, int fontSize,
                                  TextAnchor anchor = TextAnchor.MiddleLeft)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        var text = go.GetComponent<Text>();
        text.font = DefaultFont;
        text.fontSize = fontSize;
        text.alignment = anchor;
        text.color = Color.white;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    // 앵커를 좌하단 기준으로 두고 위치·크기를 픽셀로 지정
    public static void SetRect(RectTransform rt, Vector2 anchor, Vector2 pivot,
                               Vector2 position, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }
}