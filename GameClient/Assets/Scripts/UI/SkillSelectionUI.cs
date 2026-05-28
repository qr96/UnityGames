using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 레벨업 시 스킬 카드들을 표시. 카드는 프리팹에서 동적 생성.
/// </summary>
public class SkillSelectionUI : MonoBehaviour
{
    [Header("UI Root")]
    [Tooltip("켜고 끌 패널 (자기 자신을 넣으면 안 됨)")]
    public GameObject root;

    [Header("Card Spawn")]
    [Tooltip("카드 프리팹 (SkillCard.cs 부착된)")]
    public SkillCard cardPrefab;

    [Tooltip("생성된 카드들이 들어갈 부모. 보통 Horizontal Layout Group 가진 컨테이너")]
    public Transform cardContainer;

    private Action<SkillInstance> currentCallback;
    private readonly List<SkillCard> spawnedCards = new List<SkillCard>();

    void Awake()
    {
        if (root != null) root.SetActive(false);
    }

    public void Show(List<SkillInstance> choices, Action<SkillInstance> onChosen)
    {
        currentCallback = onChosen;

        ClearCards();

        foreach (var inst in choices)
        {
            SkillCard card = Instantiate(cardPrefab, cardContainer);
            card.Setup(inst, OnCardClicked);
            spawnedCards.Add(card);
        }

        if (root != null) root.SetActive(true);
        Time.timeScale = 0f;
    }

    void OnCardClicked(SkillInstance inst)
    {
        Time.timeScale = 1f;
        if (root != null) root.SetActive(false);
        ClearCards();

        currentCallback?.Invoke(inst);
        currentCallback = null;
    }

    void ClearCards()
    {
        foreach (var card in spawnedCards)
            if (card != null) Destroy(card.gameObject);
        spawnedCards.Clear();
    }
}