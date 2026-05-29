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
        Time.timeScale = 0f;
    }

    void OnCardClicked(ISelectableChoice c)
    {
        Time.timeScale = 1f;
        if (root != null) root.SetActive(false);
        ClearCards();

        currentCallback?.Invoke(c);
        currentCallback = null;
    }

    void ClearCards()
    {
        foreach (var card in spawnedCards)
            if (card != null) Destroy(card.gameObject);
        spawnedCards.Clear();
    }
}