using System.Collections.Generic;
using UnityEngine;
using AutoBattler.Battle;
using AutoBattler.Core;
using AutoBattler.Heroes;
using AutoBattler.Rounds;

namespace AutoBattler.UI
{
    /// <summary>
    /// 하단에 항상 표시되는 영웅 카드 패널.
    /// - Roster의 영웅 수만큼 HeroCard 인스턴스를 생성/갱신
    /// - 전투 시작/종료 시 카드를 BattleUnit과 연결/해제 (HP 갱신용)
    /// - 슬롯 탭 → RunManager.EquipSkill 호출
    /// - 인벤토리에서 SelectedSkill을 사용 (LoadoutScreen의 선택 상태 공유)
    /// </summary>
    public class HeroCardPanel : MonoBehaviour
    {
        [Header("매니저")]
        public RunManager runManager;
        public BattleField battleField;

        [Header("UI")]
        public Transform cardsRoot;     // HorizontalLayoutGroup 권장
        public HeroCard heroCardPrefab;

        [Header("연결 (선택)")]
        [Tooltip("LoadoutScreen 참조 — 인벤토리에서 선택된 스킬을 슬롯에 장착할 때 사용. " +
                 "비워두면 슬롯 탭은 슬롯 해제로만 동작.")]
        public LoadoutScreen loadoutScreen;

        // 카드 인스턴스 (Hero ↔ HeroCard)
        private readonly Dictionary<Hero, HeroCard> _cards = new Dictionary<Hero, HeroCard>();

        // ─────────────────────────────────────────────────────────
        private void OnEnable()
        {
            if (runManager == null) return;
            runManager.OnRoundStarted += HandleRoundStarted;
            runManager.OnRoundEnded += HandleRoundEnded;
            runManager.OnPlacementReady += Rebuild;
            runManager.SkillInv.OnChanged += RefreshAllCards;
        }

        private void OnDisable()
        {
            if (runManager == null) return;
            runManager.OnRoundStarted -= HandleRoundStarted;
            runManager.OnRoundEnded -= HandleRoundEnded;
            runManager.OnPlacementReady -= Rebuild;
            runManager.SkillInv.OnChanged -= RefreshAllCards;
        }

        // ─────────────────────────────────────────────────────────
        public void Rebuild()
        {
            if (runManager == null || heroCardPrefab == null || cardsRoot == null) return;

            // 기존 카드 모두 제거 후 다시 생성
            foreach (var c in _cards.Values) if (c != null) Destroy(c.gameObject);
            _cards.Clear();

            foreach (var hero in runManager.Roster)
            {
                var card = Instantiate(heroCardPrefab, cardsRoot);
                card.Bind(this, hero);
                _cards[hero] = card;
            }
        }

        private void RefreshAllCards()
        {
            foreach (var c in _cards.Values) c?.Refresh();
        }

        // ─────────────────────────────────────────────────────────
        // 전투 연결
        // ─────────────────────────────────────────────────────────
        private void HandleRoundStarted(int round)
        {
            // BattleField가 영웅들 스폰한 직후 — 카드를 BattleUnit과 연결
            foreach (var u in battleField.AllAllyUnits())
            {
                if (u.SourceHero != null && _cards.TryGetValue(u.SourceHero, out var card))
                    card.TrackUnit(u);
            }
        }

        private void HandleRoundEnded(int round, bool won)
        {
            foreach (var c in _cards.Values) c?.Untrack();
            RefreshAllCards();
        }

        // ─────────────────────────────────────────────────────────
        // 슬롯 탭 처리 (HeroCard가 호출)
        // ─────────────────────────────────────────────────────────
        public void OnSlotClicked(Hero hero, RunManager.LoadoutSlot slot)
        {
            if (runManager == null) return;

            // 전투 중 거부
            if (battleField != null && battleField.State == BattleState.Running) return;

            var selected = loadoutScreen != null ? loadoutScreen.SelectedSkill : null;
            if (selected != null)
            {
                runManager.EquipSkill(hero, selected, slot);
                loadoutScreen.SelectSkill(null);
            }
            else
            {
                // 선택된 스킬 없음 → 해당 슬롯 해제
                runManager.UnequipSkill(hero, slot);
            }

            if (_cards.TryGetValue(hero, out var card)) card?.Refresh();
        }
    }
}