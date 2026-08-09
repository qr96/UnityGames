using UnityEngine;

// 개발용 성능 표시(throwaway). FPS와 씬 규모를 보여준다.
// 프레임이 떨어질 때 무엇이 많은지 바로 보이게 하는 용도.
public class PerfHud : MonoBehaviour
{
    [SerializeField] private KeyCode toggleKey = KeyCode.F1;
    [SerializeField] private bool visible = true;

    private float fps;
    private float smoothing = 0.1f;
    private int rendererCount;
    private int guiComponentCount;
    private float nextCount;

    private GUIStyle style;

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey)) visible = !visible;

        float current = 1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime);
        fps = Mathf.Lerp(fps, current, smoothing);

        // 무거운 집계는 2초에 한 번만
        if (Time.unscaledTime >= nextCount)
        {
            nextCount = Time.unscaledTime + 2f;
            rendererCount = FindObjectsOfType<Renderer>().Length;

            guiComponentCount = 0;
            MonoBehaviour[] all = FindObjectsOfType<MonoBehaviour>();
            for (int i = 0; i < all.Length; i++)
            {
                var t = all[i].GetType();
                if (t.GetMethod("OnGUI",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public) != null)
                    guiComponentCount++;
            }
        }
    }

    private void OnGUI()
    {
        if (!visible) return;

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            style.normal.textColor = Color.white;
        }

        string text =
            $"FPS {fps:0}  ({Time.unscaledDeltaTime * 1000f:0.0} ms)\n" +
            $"Renderer {rendererCount}개\n" +
            $"OnGUI 컴포넌트 {guiComponentCount}개\n" +
            $"F1: 표시 전환";

        GUI.Box(new Rect(Screen.width - 210f, Screen.height - 96f, 200f, 84f), GUIContent.none);
        GUI.Label(new Rect(Screen.width - 202f, Screen.height - 92f, 194f, 80f), text, style);
    }
}
