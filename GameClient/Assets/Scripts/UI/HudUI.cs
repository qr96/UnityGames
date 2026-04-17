using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDUI : MonoBehaviour
{
    [Header("HP")]
    [SerializeField] Slider _hpBar;
    [SerializeField] TextMeshProUGUI _hpText;

    [Header("경험치")]
    [SerializeField] Slider _xpBar;

    [Header("레벨")]
    [SerializeField] TextMeshProUGUI _levelText;

    [Header("골드")]
    [SerializeField] TextMeshProUGUI _goldText;

    void Start()
    {
        // PlayerStats 이벤트 구독
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnHPChanged += UpdateHP;
            PlayerStats.Instance.OnXPChanged += UpdateXP;
            PlayerStats.Instance.OnLevelUp += UpdateLevel;

            // 초기값 세팅
            UpdateHP(PlayerStats.Instance.CurrentHP, PlayerStats.Instance.TotalMaxHP);
            UpdateXP(PlayerStats.Instance.CurrentXP, PlayerStats.Instance.GetRequiredXPForCurrentLevel());
            UpdateLevel(PlayerStats.Instance.Level);
        }
        else
        {
            Debug.LogWarning("[HUD] PlayerStats 인스턴스가 없습니다.");
        }

        // PlayerGold 이벤트 구독
        if (PlayerGold.Instance != null)
        {
            PlayerGold.Instance.OnGoldChanged += UpdateGold;
            UpdateGold(PlayerGold.Instance.Gold);
        }
        else
        {
            Debug.LogWarning("[HUD] PlayerGold 인스턴스가 없습니다.");
        }
    }

    void OnDestroy()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnHPChanged -= UpdateHP;
            PlayerStats.Instance.OnXPChanged -= UpdateXP;
            PlayerStats.Instance.OnLevelUp -= UpdateLevel;
        }

        if (PlayerGold.Instance != null)
            PlayerGold.Instance.OnGoldChanged -= UpdateGold;
    }

    // ── 갱신 ──────────────────────────────────────────────────────────────

    void UpdateHP(int current, int max)
    {
        if (_hpBar != null)
        {
            _hpBar.maxValue = max;
            _hpBar.value = current;
        }

        if (_hpText != null)
            _hpText.text = $"{current} / {max}";
    }

    void UpdateXP(int current, int required)
    {
        if (_xpBar == null) return;
        _xpBar.maxValue = required;
        _xpBar.value = current;
    }

    void UpdateLevel(int level)
    {
        if (_levelText != null)
            _levelText.text = $"Lv. {level}";
    }

    void UpdateGold(int gold)
    {
        if (_goldText != null)
            _goldText.text = $"{gold:N0} G";
    }
}
