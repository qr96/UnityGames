using System;
using System.Collections.Generic;
using UnityEngine;

public class SkillSelectionUI : MonoBehaviour
{
    [Header("UI Root")]
    public GameObject root;

    [Header("Card Spawn")]
    public SkillCard cardPrefab;
    public Transform cardContainer;

    private Action<ISelectableChoice> currentCallback;
    private readonly List<SkillCard> spawnedCards = new();

    void Awake()
    {
        if (root != null) root.SetActive(false);
    }

    public void Show(List<ISelectableChoice> choices, Action<ISelectableChoice> onChosen)
    {
        currentCallback = onChosen;

        ClearCards();

        foreach (var c in choices)
        {
            SkillCard card = Instantiate(cardPrefab, cardContainer);
            card.Setup(c, OnCardClicked);
            spawnedCards.Add(card);
        }

        if (root != null) root.SetActive(true);

        if (GameManager.Instance != null) GameManager.Instance.NotifyLevelUpStarted();
        else Time.timeScale = 0f;
    }

    void OnCardClicked(ISelectableChoice c)
    {
        if (root != null) root.SetActive(false);
        ClearCards();

        // 선택 효과 먼저 적용한 뒤 상태 복귀
        currentCallback?.Invoke(c);
        currentCallback = null;

        if (GameManager.Instance != null) GameManager.Instance.NotifyLevelUpFinished();
        else Time.timeScale = 1f;
    }

    void ClearCards()
    {
        foreach (var card in spawnedCards)
            if (card != null) Destroy(card.gameObject);
        spawnedCards.Clear();
    }
}