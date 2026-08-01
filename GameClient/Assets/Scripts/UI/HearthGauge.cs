using UnityEngine;

// 화로 머리 위에 연료 막대 + 연료 수치 표시. 씬의 모든 화로를 대상으로 함.
// 표기는 기본 수치(연료/용량), 필요 시 인스펙터에서 시간 표기로 전환 가능.
// 임시 그래픽(OnGUI) — 폴리싱 단계에서 교체.
public class HearthGauge : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.2f, 0f);
    [SerializeField] private float barWidth = 96f;
    [SerializeField] private float barHeight = 10f;

    public enum Readout { FuelNumber, RemainingTime }

    [Header("표기")]
    [SerializeField] private Readout readout = Readout.FuelNumber;

    [Header("경고")]
    [Tooltip("남은 시간이 이 값(초) 밑이면 막대를 경고색으로")]
    [SerializeField] private float warnSeconds = 30f;

    private GUIStyle labelStyle;
    private Texture2D fillTex;
    private Texture2D backTex;

    private void Start()
    {
        if (cam == null) cam = Camera.main;
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
        if (cam == null) { cam = Camera.main; if (cam == null) return; }

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleCenter };
            labelStyle.normal.textColor = Color.white;
            backTex = SolidTexture(new Color(0f, 0f, 0f, 0.55f));
            fillTex = SolidTexture(Color.white);
        }

        var list = Hearth.All;
        for (int i = 0; i < list.Count; i++)
        {
            Hearth h = list[i];
            if (h == null) continue;

            Vector3 sp = cam.WorldToScreenPoint(h.transform.position + worldOffset);
            if (sp.z < 0f) continue;

            float x = sp.x - barWidth * 0.5f;
            float y = Screen.height - sp.y;

            // 배경
            GUI.DrawTexture(new Rect(x, y, barWidth, barHeight), backTex);

            // 채움
            float ratio = Mathf.Clamp01(h.FuelRatio);
            Color fill;
            if (!h.IsLit) fill = new Color(0.45f, 0.45f, 0.5f); // 꺼짐: 회색
            else if (h.RemainingSeconds <= warnSeconds) fill = new Color(1f, 0.35f, 0.25f); // 경고: 붉은색
            else fill = new Color(1f, 0.65f, 0.2f);   // 정상: 주황

            Color prev = GUI.color;
            GUI.color = fill;
            GUI.DrawTexture(new Rect(x, y, barWidth * ratio, barHeight), fillTex);
            GUI.color = prev;

            // 라벨
            string text;
            if (!h.IsLit) text = "꺼짐";
            else if (readout == Readout.RemainingTime)
                text = HearthInteractable.FormatTime(h.RemainingSeconds);
            else
                text = $"연료 {Mathf.FloorToInt(h.Fuel)}/{Mathf.FloorToInt(h.FuelCapacity)}";
            GUI.Label(new Rect(x, y + barHeight, barWidth, 18f), text, labelStyle);
        }
    }
}