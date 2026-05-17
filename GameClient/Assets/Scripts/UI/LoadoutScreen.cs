using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AutoBattler.Data;
using AutoBattler.Heroes;
using AutoBattler.Rounds;

namespace AutoBattler.UI
{
    /// <summary>
    /// 장착/합성 화면.
    /// - 좌: 영웅 카드 N개. 각 카드에 스킬 A/B 슬롯 버튼.
    /// - 우: 인벤토리 스킬 목록. 카운트와 함께 표시. 3개면 [합성] 버튼.
    ///
    /// 사용 방법:
    ///   1) "장착할 스킬"을 인벤토리에서 탭 → 선택 상태
    ///   2) 영웅 카드의 슬롯 A 또는 B 탭 → 장착 (기존 스킬은 인벤토리로 복귀)
    ///   3) 합성 가능한 스킬 옆 [합성] 버튼 → 3개 소비 + 상위 1개 추가
    ///   4) [확인] 버튼 → RunManager.ConfirmLoadout() → 배치 화면
    ///
    /// 프리팹 의존:
    ///   heroCardPrefab: HeroLoadoutCard 컴포넌트가 붙은 프리팹
    ///   inventoryItemPrefab: InventorySkillItem 컴포넌트가 붙은 프리팹
    /// </summary>
    public class LoadoutScreen : MonoBehaviour
    {
        [Header("매니저")]
        public RunManager runManager;

        [Header("UI 루트")]
        public Transform heroCardsRoot;     // VerticalLayoutGroup 권장
        public Transform inventoryRoot;     // VerticalLayoutGroup 권장
        public Button proceedButton;      // "다음 전투"

        [Header("프리팹")]
        public HeroLoadoutCard heroCardPrefab;
        public InventorySkillItem inventoryItemPrefab;

        // 런타임 인스턴스
        private readonly List<HeroLoadoutCard> _heroCards = new List<HeroLoadoutCard>();
        private readonly List<InventorySkillItem> _invItems = new List<InventorySkillItem>();

        // 현재 인벤토리에서 선택된 스킬
        public SkillData SelectedSkill { get; private set; }

        // ─────────────────────────────────────────────────────────
        private void Awake()
        {
            if (proceedButton != null)
                proceedButton.onClick.AddListener(OnProceedClicked);
        }

        private void OnEnable()
        {
            if (runManager != null)
                runManager.SkillInv.OnChanged += RebuildInventory;
            Rebuild();
        }

        private void OnDisable()
        {
            if (runManager != null)
                runManager.SkillInv.OnChanged -= RebuildInventory;
        }

        // ─────────────────────────────────────────────────────────
        public void Rebuild()
        {
            RebuildHeroCards();
            RebuildInventory();
        }

        private void RebuildHeroCards()
        {
            foreach (var c in _heroCards) if (c != null) Destroy(c.gameObject);
            _heroCards.Clear();

            if (runManager == null || heroCardPrefab == null || heroCardsRoot == null) return;

            foreach (var hero in runManager.Roster)
            {
                var card = Instantiate(heroCardPrefab, heroCardsRoot);
                card.Bind(this, hero);
                _heroCards.Add(card);
            }
        }

        private void RebuildInventory()
        {
            foreach (var i in _invItems) if (i != null) Destroy(i.gameObject);
            _invItems.Clear();

            if (runManager == null || inventoryItemPrefab == null || inventoryRoot == null) return;

            foreach (var kv in runManager.SkillInv.Counts)
            {
                var item = Instantiate(inventoryItemPrefab, inventoryRoot);
                item.Bind(this, kv.Key, kv.Value);
                _invItems.Add(item);
            }

            // 영웅 카드도 같이 갱신 (장착 해제 등 변경 반영)
            foreach (var c in _heroCards) c?.Refresh();
        }

        // ─────────────────────────────────────────────────────────
        // 자식 컴포넌트가 호출하는 API
        // ─────────────────────────────────────────────────────────
        public void SelectSkill(SkillData s)
        {
            SelectedSkill = s;
            foreach (var i in _invItems) i?.UpdateSelectedHighlight();
        }

        public void EquipSelectedTo(Hero hero, RunManager.LoadoutSlot slot)
        {
            if (SelectedSkill == null) return;
            runManager.EquipSkill(hero, SelectedSkill, slot);
            // 선택 해제 (UX: 한 번 장착하면 선택 풀림)
            SelectSkill(null);
            // 인벤토리/카드 갱신은 SkillInv.OnChanged 로 자동
        }

        public void UnequipFrom(Hero hero, RunManager.LoadoutSlot slot)
        {
            runManager.UnequipSkill(hero, slot);
        }

        public void FuseSkill(SkillData s)
        {
            runManager.SkillInv.TryFuse(s);
        }

        // ─────────────────────────────────────────────────────────
        private void OnProceedClicked()
        {
            runManager.ConfirmLoadout();   // → OnPlacementReady → 배치 화면
        }
    }
}