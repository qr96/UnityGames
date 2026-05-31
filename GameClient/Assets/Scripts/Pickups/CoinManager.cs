using System;
using System.Text;
using TMPro;
using UnityEngine;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }

    [Header("UI")]
    public TMP_Text coinText;
    public string uiPrefix = "Coin: ";

    [Header("Combo")]
    public float comboWindow = 1.5f;
    public float maxComboMultiplier = 4f;
    public float comboStep = 0.1f;

    public int TotalCoins { get; private set; }
    public int CurrentCombo { get; private set; }

    public event Action<int, int, float> OnCoinCollected;

    private float lastCollectTime = -999f;
    private readonly StringBuilder sb = new StringBuilder(32);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        UpdateUI();
    }

    void Update()
    {
        if (CurrentCombo > 0 && Time.time - lastCollectTime > comboWindow)
            CurrentCombo = 0;
    }

    public void AddCoin(int amount)
    {
        if (amount <= 0) return;

        if (Time.time - lastCollectTime <= comboWindow) CurrentCombo++;
        else CurrentCombo = 1;
        lastCollectTime = Time.time;

        float multiplier = Mathf.Min(1f + (CurrentCombo - 1) * comboStep, maxComboMultiplier);
        int gained = Mathf.RoundToInt(amount * multiplier);

        TotalCoins += gained;
        UpdateUI();

        OnCoinCollected?.Invoke(gained, TotalCoins, multiplier);
    }

    public bool TrySpend(int amount)
    {
        if (amount <= 0 || TotalCoins < amount) return false;
        TotalCoins -= amount;
        UpdateUI();
        return true;
    }

    public void ResetCoins()
    {
        TotalCoins = 0;
        CurrentCombo = 0;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (coinText == null) return;
        sb.Length = 0;
        sb.Append(uiPrefix).Append(TotalCoins);
        coinText.SetText(sb);
    }
}