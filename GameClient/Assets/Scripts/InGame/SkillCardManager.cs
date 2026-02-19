using GameDefine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InGame
{
    public class SkillCardManager : MonoBehaviour
    {
        public static SkillCardManager Instance;

        public Action<List<SkillCardData>> OnUpdateCardData;
        public Action<BaseUnit> OnSpawnPlayer;
        public Action<List<BaseUnit>> OnSpawnEnemies;
        public Action<BaseUnit> OnUpdatePlayer;
        public Action<List<BaseUnit>> OnUpdateEnemies;

        List<SkillCardData> cards = new List<SkillCardData>();
        BaseUnit player;
        List<BaseUnit> enemies = new List<BaseUnit>();

        int currentAP;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            player = new BaseUnit(new Stat() { attack = 10, hp = 100 });
            player.Spawn();
            OnSpawnPlayer?.Invoke(player);

            for (int i = 0; i < 3; i++)
            {
                var enemy = new BaseUnit(new Stat() { hp = 100, attack = 2 });
                enemies.Add(enemy);
                enemy.Spawn();
            }

            OnSpawnEnemies?.Invoke(enemies);
            OnStartTurn();
        }

        // ─────────────────────────────────────────
        // 턴
        // ─────────────────────────────────────────
        void OnStartTurn()
        {
            currentAP = 3;
            DealCards();
        }

        void ConsumeAP()
        {
            currentAP--;
            if (currentAP <= 0)
                StartCoroutine(DelayNextTurn());
        }

        IEnumerator DelayNextTurn()
        {
            yield return new WaitForSeconds(1f);
            OnStartTurn();
        }

        // ─────────────────────────────────────────
        // 카드 관리
        // ─────────────────────────────────────────
        void DealCards()
        {
            while (cards.Count < Common.MaxCardCount)
            {
                var rand = UnityEngine.Random.Range(0, 6);
                AddCard(new SkillCardData(rand));
            }
        }

        public void AddCard(SkillCardData newCard)
        {
            if (cards.Count >= Common.MaxCardCount)
            {
                Debug.Log("손패가 가득 찼습니다.");
                return;
            }

            cards.Add(newCard);
            CheckCombineCards();
            OnUpdateCardData?.Invoke(cards);
        }

        public void MoveCard(SkillCardData moveData, int targetIndex)
        {
            int currentIndex = cards.IndexOf(moveData);
            if (currentIndex == -1 || currentIndex == targetIndex) return;

            cards.RemoveAt(currentIndex);
            cards.Insert(Mathf.Clamp(targetIndex, 0, cards.Count), moveData);

            CheckCombineCards();
            OnUpdateCardData?.Invoke(cards);

            ConsumeAP();
        }

        public void UseCard(SkillCardData cardData, BaseUnit target)
        {
            int index = cards.IndexOf(cardData);
            if (index == -1) return;

            cards.RemoveAt(index);

            var logic = SkillLogicManager.Instance.GetLogic(cardData.skillId);
            logic.Execute(player, enemies, target, cardData.rank);

            CheckCombineCards();
            OnUpdateCardData?.Invoke(cards);
            OnUpdatePlayer?.Invoke(player);
            OnUpdateEnemies?.Invoke(enemies);

            Debug.Log($"스킬 {cardData.skillId} (랭크 {cardData.rank}) 사용 | 남은 AP: {currentAP - 1}");

            ConsumeAP();
        }

        void CheckCombineCards()
        {
            bool combined = true;
            while (combined)
            {
                combined = false;
                for (int i = 0; i < cards.Count - 1; i++)
                {
                    if (cards[i].skillId == cards[i + 1].skillId &&
                        cards[i].rank == cards[i + 1].rank &&
                        cards[i].rank < 3)
                    {
                        cards[i].rank++;
                        cards.RemoveAt(i + 1);
                        combined = true;
                        break;
                    }
                }
            }
        }
    }
}
