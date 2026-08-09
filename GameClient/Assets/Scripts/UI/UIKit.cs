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

    // 링(도넛) 스프라이트를 코드로 만든다. innerRatio 0이면 꽉 찬 원.
    public static Sprite CreateRingSprite(int size = 128, float innerRatio = 0.62f)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };

        float half = size * 0.5f;
        float outer = half - 1f;
        float inner = outer * Mathf.Clamp01(innerRatio);
        var pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - half;
                float dy = y + 0.5f - half;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                // 가장자리 1px 부드럽게
                float a = Mathf.Clamp01(outer - d) * Mathf.Clamp01(d - inner);
                if (innerRatio <= 0f) a = Mathf.Clamp01(outer - d);

                byte alpha = (byte)(Mathf.Clamp01(a) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }

        tex.SetPixels32(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
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