using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AutoBattler.Data;
using AutoBattler.Rounds;

namespace AutoBattler.UI
{
    /// <summary>
    /// 장착/합성 화면. 인벤토리만 담당.
    /// 영웅 카드는 항상 보이는 HeroCardPanel에서 처리됨.
    ///
    /// 사용 흐름:
    ///   1) 인벤토리에서 스킬 탭 → SelectedSkill 설정
    ///   2) (HeroCardPanel이 영웅 슬롯 탭 처리, 여기서는 SelectedSkill만 노출)
    ///   3) 합성 가능한 스킬 옆 [합성] 버튼 → 3개 소비 + 상위 1개 추가
    ///   4) [확인] 버튼 → RunManager.ConfirmLoadout() → 배치 화면
    ///
    /// 프리팹 의존:
    ///   inventoryItemPrefab: InventorySkillItem 컴포넌트가 붙은 프리팹
    /// </summary>
    public class LoadoutScreen : MonoBehaviour
    {
        [Header("매니저")]
        public RunManager runManager;

        [Header("UI 루트")]
        public Transform inventoryRoot;     // VerticalLayoutGroup 권장
        public Button proceedButton;      // "확인" — 다음 단계(배치)로

        [Header("프리팹")]
        public InventorySkillItem inventoryItemPrefab;

        // 런타임 인스턴스
        private readonly List<InventorySkillItem> _invItems = new List<InventorySkillItem>();

        // 현재 인벤토리에서 선택된 스킬 (HeroCardPanel이 읽음)
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
            RebuildInventory();
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
        }

        // ─────────────────────────────────────────────────────────
        // 자식 컴포넌트가 호출하는 API
        // ─────────────────────────────────────────────────────────
        public void SelectSkill(SkillData s)
        {
            SelectedSkill = s;
            foreach (var i in _invItems) i?.UpdateSelectedHighlight();
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