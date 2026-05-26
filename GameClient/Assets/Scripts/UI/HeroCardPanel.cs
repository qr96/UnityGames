using System.Collections.Generic;
using UnityEngine;
using AutoBattler.Battle;
using AutoBattler.Heroes;
using AutoBattler.Rounds;
using AutoBattler.Core;

namespace AutoBattler.UI
{
    /// <summary>
    /// 하단에 항상 표시되는 영웅 카드 패널.
    /// - Roster의 영웅 수만큼 HeroCard 인스턴스를 생성/갱신
    /// - 슬롯 탭 → RunManager.EquipSkill 호출
    /// - 인벤토리에서 SelectedSkill을 사용 (LoadoutScreen의 선택 상태 공유)
    ///
    /// 설계 노트:
    ///   HP는 Hero가 소유하고 카드가 Hero.OnHPChanged를 직접 구독하므로,
    ///   패널은 전투 시작/종료 시점에 카드를 BattleUnit과 연결할 필요가 없다.
    ///   battleField 참조는 "전투 중 슬롯 조작 차단" 용도로만 남아있음.
    /// </summary>
    public class HeroCardPanel : MonoBehaviour
    {
        [Header("매니저")]
        public RunManager runManager;
        [Tooltip("전투 중 슬롯 조작을 막기 위한 참조. HP 표시에는 사용하지 않음.")]
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
            runManager.OnPlacementReady += Rebuild;
            runManager.SkillInv.OnChanged += RefreshAllCards;
        }

        private void OnDisable()
        {
            if (runManager == null) return;
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
                card.Bind(this, hero);   // 카드가 Hero.OnHPChanged를 구독
                _cards[hero] = card;
            }
        }

        private void RefreshAllCards()
        {
            foreach (var c in _cards.Values) c?.Refresh();
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