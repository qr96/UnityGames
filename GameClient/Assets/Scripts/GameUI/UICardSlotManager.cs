using GameDefine;
using GameUI;
using InGame;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UICardSlotManager : MonoBehaviour
{
    public List<UISkillCard> cards = new List<UISkillCard>();
    public List<Button> enemies = new List<Button>();
    public Dictionary<BaseUnit, GuageBar> enemyHpBarDic = new Dictionary<BaseUnit, GuageBar>();
    public GuageBar playerHpBar;

    public Button enemyPrefab;
    public GuageBar hpBarPrefab;
    public RectTransform targetMark;

    int putIndex;
    BaseUnit targetUnit;

    private void Awake()
    {
        enemyPrefab.gameObject.SetActive(false);
        hpBarPrefab.gameObject.SetActive(false);

        OnSelectTarget(-1, null);

        SkillCardManager.Instance.OnUpdateCardData += OnUpdateCardData;
        SkillCardManager.Instance.OnSpawnPlayer += OnSpawnPlayer;
        SkillCardManager.Instance.OnUpdatePlayer += OnUpdatePlayer;
        SkillCardManager.Instance.OnSpawnEnemies += OnSpawnEnemies;
        SkillCardManager.Instance.OnUpdateEnemies += OnUpdateEnemies;
    }

    void AlignmentCard()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            card.rectTransform.localPosition = GetCardPosition(i);
        }
    }

    void OnSelectTarget(int index, BaseUnit targetUnit)
    {
        Debug.Log($"OnSelectTarget() index={index}");

        if (this.targetUnit == targetUnit || index == -1 || targetUnit == null)
        {
            index = -1;
            this.targetUnit = null;
            targetMark.gameObject.SetActive(false);
        }
        else
        {
            this.targetUnit = targetUnit;
            targetMark.gameObject.SetActive(true);
            targetMark.anchoredPosition = enemies[index].GetComponent<RectTransform>().anchoredPosition;
            targetMark.SetAsLastSibling();
        }
    }

    Vector2 GetCardPosition(int index)
    {
        return new Vector2(-300f + 100f * index, -400f);
    }

    int GetNearSlotIndex(Vector2 pos)
    {
        var minIndex = 0;
        var minDis = Vector2.SqrMagnitude(pos - GetCardPosition(minIndex));

        for (int i = 1; i < Common.MaxCardCount; i++)
        {
            var sqrDis = Vector2.SqrMagnitude(pos - GetCardPosition(i));
            if (sqrDis < minDis)
            {
                minDis = sqrDis;
                minIndex = i;
            }
        }

        return minIndex;
    }

    public void OnUpdateCardData(List<SkillCardData> cardDataList)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            card.gameObject.SetActive(false);
        }

        for (int i = 0; i < cardDataList.Count; i++)
        {
            var card = cards[i];
            var cardData = cardDataList[i];
            card.gameObject.SetActive(true);
            card.info.text = cardData.skillId.ToString();
            card.rank.text = cardData.rank.ToString();
            card.cardData = cardData;
            card.manager = this;
        }

        Debug.Log(cardDataList.GetCardInfo());
        AlignmentCard();
    }

    public void OnSpawnPlayer(BaseUnit unit)
    {
        if (playerHpBar == null)
            playerHpBar = Instantiate(hpBarPrefab, hpBarPrefab.transform.parent);

        playerHpBar.SetPosition(new Vector2(-240f, 300f));
        playerHpBar.gameObject.SetActive(true);
        playerHpBar.SetGuage(unit.originStat.hp, unit.nowStat.hp);
    }

    public void OnUpdatePlayer(BaseUnit unit)
    {
        if (playerHpBar != null)
        {
            playerHpBar.SetGuage(unit.originStat.hp, unit.nowStat.hp);
        }
    }

    public void OnSpawnEnemies(List<BaseUnit> units)
    {
        foreach (var enemy in enemies)
        {
            Destroy(enemy);
        }

        foreach (var hpBar in enemyHpBarDic)
        {
            Destroy(hpBar.Value);
        }

        enemies.Clear();
        enemyHpBarDic.Clear();

        for (int i = 0; i < units.Count; i++)
        {
            var enemyButton = Instantiate(enemyPrefab, enemyPrefab.transform.parent);
            var index = i;
            enemies.Add(enemyButton);
            enemyButton.gameObject.SetActive(true);
            enemyButton.GetComponent<RectTransform>().anchoredPosition = enemyPrefab.GetComponent<RectTransform>().anchoredPosition + new Vector2(i * 140f, 0f);
            enemyButton.onClick.RemoveAllListeners();
            enemyButton.onClick.AddListener(() => OnSelectTarget(index, units[index]));

            var enemyHpBar = Instantiate(hpBarPrefab, hpBarPrefab.transform.parent);
            enemyHpBarDic.Add(units[index], enemyHpBar);
            enemyHpBar.gameObject.SetActive(true);
            enemyHpBar.SetGuage(units[index].originStat.hp, units[index].nowStat.hp);
            enemyHpBar.SetPosition(enemyButton.GetComponent<RectTransform>().anchoredPosition + new Vector2(0f, 100f));
        }
    }

    void OnUpdateEnemies(List<BaseUnit> units)
    {
        foreach (var unit in units)
        {
            if (enemyHpBarDic.ContainsKey(unit))
            {
                enemyHpBarDic[unit].SetGuage(unit.originStat.hp, unit.nowStat.hp);
            }
            else
            {
                Debug.LogError($"OnUpdateEnemies() Failed to find unit");
            }
        }
    }

    public void OnDrag(UISkillCard targetCard)
    {
        var nearIndex = GetNearSlotIndex(targetCard.rectTransform.localPosition);
        putIndex = nearIndex;

        cards.Remove(targetCard);
        cards.Insert(nearIndex, targetCard);

        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            if (targetCard == card)
                continue;

            card.rectTransform.localPosition = GetCardPosition(i);
        }
    }

    public void OnEndDrag(UISkillCard targetCard)
    {
        AlignmentCard();

        if (targetCard.cardData != null)
        {
            SkillCardManager.Instance.MoveCard(targetCard.cardData, putIndex);
        }
        else
        {
            Debug.LogError($"OnEndDrag() cardData is null. targetCard:{targetCard.name}");
        }
    }

    public void OnClick(SkillCardData targetCard)
    {
        SkillCardManager.Instance.UseCard(targetCard, targetUnit);
    }
}
