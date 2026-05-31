using UnityEngine;
using TMPro;

/// <summary>
/// 데미지 숫자 팝업. 풀에서 꺼내져 Play()로 시작.
/// 발생 위치(월드)를 화면 좌표로 잡고, 화면상에서 위로 떠오르며 페이드.
/// 끝나면 풀 반납.
/// </summary>
[RequireComponent(typeof(Poolable))]
public class DamagePopupUI : WorldFollowUI
{
    [Header("References")]
    public TMP_Text label;

    [Header("Motion")]
    [Tooltip("화면상 떠오르는 속도(픽셀/초)")]
    public float riseSpeedScreen = 80f;

    [Tooltip("표시 시간(초)")]
    public float duration = 0.7f;

    [Header("Color")]
    public Color normalColor = Color.white;
    public Color criticalColor = new Color(1f, 0.85f, 0.2f);

    private Poolable poolable;
    private float elapsed;
    private bool playing;
    private Color baseColor;

    protected override void Awake()
    {
        base.Awake();
        poolable = GetComponent<Poolable>();
        if (label == null) label = GetComponentInChildren<TMP_Text>();
    }

    public void Play(int amount, Vector3 spawnWorldPos, bool isCritical)
    {
        SetWorldPosition(spawnWorldPos);  // 발생 지점 고정
        elapsed = 0f;
        playing = true;
        screenOffset = Vector2.zero;

        if (label != null)
        {
            label.SetText("{0}", amount);
            baseColor = isCritical ? criticalColor : normalColor;
            label.color = baseColor;
        }
    }

    // 팝업은 고정 월드 위치 + 화면상 상승 오프셋.
    protected override void LateUpdate()
    {
        if (!playing) return;

        elapsed += Time.deltaTime;

        // 화면상 상승 오프셋 누적 (베이스가 위치 계산 시 반영)
        screenOffset += Vector2.up * riseSpeedScreen * Time.deltaTime;

        // 베이스가 월드 위치 + screenOffset으로 최종 위치 갱신
        UpdateScreenPosition();

        if (label != null)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            Color c = baseColor;
            c.a = 1f - t;
            label.color = c;
        }

        if (elapsed >= duration)
        {
            playing = false;
            poolable.ReleaseSelf();
        }
    }
}