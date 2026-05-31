using UnityEngine;

/// <summary>
/// 월드 공간의 한 지점(또는 Transform)을 따라다니는 Screen Space UI 요소.
/// WorldToScreenPoint로 화면 픽셀을 구해 부모 Canvas 기준 anchoredPosition으로 변환.
/// Screen Space - Overlay / Camera 둘 다 RectTransformUtility로 안전하게 처리.
/// 체력바/데미지팝업의 공통 베이스.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public abstract class WorldFollowUI : MonoBehaviour
{
    protected RectTransform rect;
    protected RectTransform canvasRect;
    protected Canvas canvas;
    protected Camera cam;

    protected Transform target;
    protected Vector3 worldPos;
    protected Vector3 worldOffset;

    // 화면 픽셀 기준 추가 오프셋 (팝업이 위로 떠오를 때 사용)
    protected Vector2 screenOffset;

    protected virtual void Awake()
    {
        rect = GetComponent<RectTransform>();
        cam = Camera.main;
        CacheCanvas();
    }

    void CacheCanvas()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null) canvasRect = canvas.transform as RectTransform;
    }

    protected void SetWorldPosition(Vector3 pos)
    {
        target = null;
        worldPos = pos;
        screenOffset = Vector2.zero;
        UpdateScreenPosition();
    }

    protected void SetTarget(Transform t, Vector3 offset)
    {
        target = t;
        worldOffset = offset;
        screenOffset = Vector2.zero;
        UpdateScreenPosition();
    }

    protected virtual void LateUpdate()
    {
        UpdateScreenPosition();
    }

    protected void UpdateScreenPosition()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;
        if (canvasRect == null) CacheCanvas();
        if (canvasRect == null) return;

        Vector3 wp = target != null ? target.position + worldOffset : worldPos;
        Vector3 screen = cam.WorldToScreenPoint(wp);

        // 카메라 뒤면 숨김
        if (screen.z < 0f)
        {
            if (rect.gameObject.activeSelf) rect.gameObject.SetActive(false);
            return;
        }
        if (!rect.gameObject.activeSelf) rect.gameObject.SetActive(true);

        // 화면 픽셀 + 오프셋
        Vector2 screenPoint = (Vector2)screen + screenOffset;

        // Overlay면 카메라 null, Camera/World면 렌더 카메라 전달
        Camera uiCam = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            ? null
            : (canvas != null ? canvas.worldCamera : null);

        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPoint, uiCam, out localPoint))
        {
            rect.localPosition = localPoint;
        }

    }
}