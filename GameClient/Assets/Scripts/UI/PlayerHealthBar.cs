using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 플레이어 체력바 (화면 고정). Slider 사용. PlayerController.OnHPChanged 구독.
/// </summary>
public class PlayerHealthBar : MonoBehaviour
{
    [Header("References")]
    public Slider slider;
    [Tooltip("선택: HP 숫자 텍스트")]
    public TMP_Text hpText;

    private bool subscribed = false;

    void OnEnable() => TrySubscribe();
    void Start() => TrySubscribe();

    void OnDisable()
    {
        if (subscribed && PlayerController.Instance != null)
        {
            PlayerController.Instance.OnHPChanged -= Refresh;
            subscribed = false;
        }
    }

    void TrySubscribe()
    {
        if (subscribed || PlayerController.Instance == null) return;
        PlayerController.Instance.OnHPChanged += Refresh;
        subscribed = true;
        Refresh();
    }

    void Refresh()
    {
        var player = PlayerController.Instance;
        if (player == null) return;

        float ratio = player.MaxHP > 0 ? (float)player.CurrentHP / player.MaxHP : 0f;
        if (slider != null) slider.value = Mathf.Clamp01(ratio);
        if (hpText != null) hpText.SetText("{0}/{1}", player.CurrentHP, player.MaxHP);
    }
}