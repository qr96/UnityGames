using UnityEngine;

// 활성 타겟 위에 "E — 행동명" 라벨 표시. 행동명 = 타겟의 Prompt.
// 임시 그래픽(OnGUI). 정식 UI는 게임필/폴리싱 단계에서 교체.
public class TargetLabel : MonoBehaviour
{
    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private Camera cam;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.5f, 0f);

    private GUIStyle style;

    private void Start()
    {
        if (interactor == null) interactor = FindObjectOfType<PlayerInteractor>();
        if (cam == null) cam = Camera.main;
    }

    private void OnGUI()
    {
        if (interactor == null || interactor.Current == null) return;
        if (cam == null) { cam = Camera.main; if (cam == null) return; }

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.box) { fontSize = 15, alignment = TextAnchor.MiddleCenter };
            style.normal.textColor = Color.white;
        }

        InteractableBase t = interactor.Current;
        Vector3 sp = cam.WorldToScreenPoint(t.Position + worldOffset);
        if (sp.z < 0f) return; // 카메라 뒤

        string text = $"E — {t.Prompt}";
        Vector2 size = style.CalcSize(new GUIContent(text));
        float w = size.x + 12f;
        float h = size.y + 6f;
        float x = sp.x - w * 0.5f;
        float y = (Screen.height - sp.y) - h; // GUI 좌표는 y 반전
        GUI.Label(new Rect(x, y, w, h), text, style);
    }
}
