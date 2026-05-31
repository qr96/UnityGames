using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExperienceManager : MonoBehaviour
{
    public static ExperienceManager Instance { get; private set; }

    [Header("Event Channel")]
    public EnemyDiedChannel diedChannel;

    [Header("UI")]
    public Slider xpBar;
    public TMP_Text levelText;

    [Header("Leveling Curve")]
    public int baseXpToLevel = 5;
    public float xpCurveMultiplier = 1.5f;

    public int CurrentLevel { get; private set; } = 1;
    public int CurrentXp { get; private set; } = 0;
    public int XpToNextLevel { get; private set; }

    public event Action<int> OnLevelUp;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        if (diedChannel != null) diedChannel.OnRaised += HandleEnemyDied;
    }

    void OnDisable()
    {
        if (diedChannel != null) diedChannel.OnRaised -= HandleEnemyDied;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        XpToNextLevel = baseXpToLevel;
        UpdateUI();
    }

    void HandleEnemyDied(EnemyDeathInfo info) => AddXp(info.xpReward);

    public void AddXp(int amount)
    {
        if (amount <= 0) return;
        CurrentXp += amount;

        while (CurrentXp >= XpToNextLevel)
        {
            CurrentXp -= XpToNextLevel;
            CurrentLevel++;
            XpToNextLevel = Mathf.RoundToInt(XpToNextLevel * xpCurveMultiplier);
            OnLevelUp?.Invoke(CurrentLevel);
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        if (xpBar != null) xpBar.value = (float)CurrentXp / XpToNextLevel;
        if (levelText != null) levelText.text = "Lv. " + CurrentLevel;
    }
}