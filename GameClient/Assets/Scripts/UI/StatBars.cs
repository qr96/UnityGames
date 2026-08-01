using UnityEngine;

// 온기 / 허기 / 기력 막대. 화면 좌하단에 표시.
// 임시 그래픽(OnGUI) — 폴리싱 단계에서 교체.
public class StatBars : MonoBehaviour
{
    [SerializeField] private PlayerStats stats; // 비우면 씬에서 찾음

    [Header("배치")]
    [SerializeField] private Vector2 margin = new Vector2(16f, 16f);
    [SerializeField] private float barWidth = 220f;
    [SerializeField] private float barHeight = 16f;
    [SerializeField] private float gap = 6f;

    [Header("색")]
    [SerializeField] private Color warmthColor = new Color(1f, 0.6f, 0.2f);
    [SerializeField] private Color hungerColor = new Color(0.6f, 0.8f, 0.35f);
    [SerializeField] private Color staminaColor = new Color(0.4f, 0.75f, 1f);
    [SerializeField] private Color lowColor = new Color(1f, 0.35f, 0.3f);
    [Tooltip("이 비율 밑이면 경고색")]
    [SerializeField] private float lowThreshold = 0.25f;

    private Texture2D fillTex;
    private Texture2D backTex;
    private GUIStyle labelStyle;

    private void Start()
    {
        if (stats == null) stats = FindObjectOfType<PlayerStats>();
    }

    private static Texture2D SolidTexture(Color c)
    {
        Texture2D t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }

    private void OnGUI()
    {
        if (stats == null) return;

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 12 };
            labelStyle.normal.textColor = Color.white;
            backTex = SolidTexture(new Color(0f, 0f, 0f, 0.55f));
            fillTex = SolidTexture(Color.white);
        }

        float totalH = barHeight * 3f + gap * 2f;
        float x = margin.x;
        float y = Screen.height - margin.y - totalH;

        DrawBar(x, y, "온기", stats.WarmthNormalized, warmthColor);
        DrawBar(x, y + (barHeight + gap), "허기", stats.HungerNormalized, hungerColor);
        DrawBar(x, y + (barHeight + gap) * 2f, "기력", stats.StaminaNormalized,
                stats.IsSprinting ? Color.Lerp(staminaColor, Color.white, 0.25f) : staminaColor);
    }

    private void DrawBar(float x, float y, string label, float ratio, Color color)
    {
        ratio = Mathf.Clamp01(ratio);

        GUI.DrawTexture(new Rect(x, y, barWidth, barHeight), backTex);

        Color prev = GUI.color;
        GUI.color = ratio <= lowThreshold ? lowColor : color;
        GUI.DrawTexture(new Rect(x, y, barWidth * ratio, barHeight), fillTex);
        GUI.color = prev;

        GUI.Label(new Rect(x + 6f, y - 1f, barWidth, barHeight + 2f),
            $"{label}  {Mathf.RoundToInt(ratio * 100f)}%", labelStyle);
    }
}
