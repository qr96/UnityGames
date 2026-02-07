using InGame;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public class InGameLayout : MonoBehaviour
    {
        public List<ElementSlot> elementSlots = new List<ElementSlot>();
        public List<ScrollSlot> scrollSlots = new List<ScrollSlot>();
        public Button rollButton;
        public Dictionary<string, Sprite> spritesDic = new Dictionary<string, Sprite>();
        public TMP_Text remainText;

        public ScrollSlot scrollPrefab;
        public Button attackButton;

        public GuageBar playerHpBar;
        public List<GuageBar> enemyHpBarList;
        public List<Button> enemyTargetButton;
        
        Dictionary<BaseUnit, GuageBar> enemyHpBarDic = new Dictionary<BaseUnit, GuageBar>();

        int selectedScrollIndex = DefaultScrollIndex; // 선택된 스크롤 인덱스
        BaseUnit selectedTarget;    // 선택된 타깃

        static int DefaultScrollIndex = -1; // 선택된 스크롤이 없는 경우 인덱스

        private void Awake()
        {
            scrollPrefab.gameObject.SetActive(false);

            LoadSprites();
            
            GameStage.Instance.OnChangeElementSlots += SetElementSlots;
            GameStage.Instance.OnChangeLock += OnChangeLockSlots;
            GameStage.Instance.OnChangeScrollSlots += OnUpdateScrollSlots;
            GameStage.Instance.OnChangeTurn += OnChangeTurn;

            GameStage.Instance.OnSpawnPlayer += OnSpawnPlayerUnit;
            GameStage.Instance.OnSpawnEnemies += OnSpawnEnemyUnits;
            GameStage.Instance.OnChangePlayerUnit += OnUpdatePlayerUnit;
            GameStage.Instance.OnChangeEnemyUnit += OnUpdateEnemyHp;

            rollButton.onClick.AddListener(() => GameStage.Instance.RollDice());
            for (int i = 0; i < Common.MaxSlotCount; i++)
            {
                var slot = elementSlots[i];
                var index = i;
                slot.button.onClick.AddListener(() => GameStage.Instance.ChangeSlotLock(index));
            }
            attackButton.onClick.AddListener(UseScroll);
        }

        private void LoadSprites()
        {
            var elements = Resources.LoadAll<Sprite>("UI/Elements000");
            foreach (var element in elements)
            {
                Debug.Log(element.name);
                spritesDic.Add(element.name, element);
            }

            var icons = Resources.LoadAll<Sprite>("UI/Icons000");
            foreach (var icon in icons)
            {
                Debug.Log(icon.name);
                spritesDic.Add(icon.name, icon);
            }
        }

        public void SetElementSlots(Common.ElementType[] slots)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                var elementSlot = elementSlots[i];
                var elementType = slots[i];
                var path = GetElementIconPath(elementType);
                elementSlot.SetIcon(spritesDic[path]);
            }
        }

        string GetElementIconPath(Common.ElementType element)
        {
            return $"Icons000_{(int)element}";
        }

        void OnChangeLockSlots(bool[] lockSlots)
        {
            for (int i = 0; i < lockSlots.Length; i++)
            {
                var isLocked = lockSlots[i];
                elementSlots[i].SetLockImage(isLocked);
            }
        }

        void OnUpdateScrollSlots(List<BaseMagicScroll> list)
        {
            foreach (var slot in scrollSlots)
                slot.gameObject.SetActive(false);

            for (int i = 0; i < list.Count; i++)
            {
                ScrollSlot slot = null;

                if (i < scrollSlots.Count)
                {
                    slot = scrollSlots[i];
                }
                else
                {
                    var scrollIndex = scrollSlots.Count;
                    slot = Instantiate(scrollPrefab, scrollPrefab.transform.parent);
                    slot.button.onClick.AddListener(() => SelectScroll(scrollIndex));
                    scrollSlots.Add(slot);
                }

                if (slot != null)
                {
                    slot.gameObject.SetActive(true);
                    // 슬롯 아이콘 설정
                }
                else
                {
                    Debug.LogError($"[InGameLayout] OnUpdateScrollSlots() slot is null. i={i}");
                }
            }
        }

        void SelectScroll(int scrollIndex)
        {
            if (selectedScrollIndex == scrollIndex)
                selectedScrollIndex = DefaultScrollIndex;
            else
                selectedScrollIndex = scrollIndex;

            for (int i = 0; i < scrollSlots.Count; i++)
            {
                var slot = scrollSlots[i];
                slot.SelectScroll(i == selectedScrollIndex);
            }
        }

        void UseScroll()
        {
            if (selectedScrollIndex < 0)
            {
                Debug.Log("[InGameLayout] UseScroll() 스크롤을 선택해주세요.");
                return;
            }
            
            if (selectedTarget == null)
            {
                Debug.Log("[InGameLayout] UseScroll() 타깃을 선택해주세요.");
                return;
            }
            
            GameStage.Instance.UseScroll(selectedScrollIndex, selectedTarget);
        }

        void OnChangeTurn(bool isPlayerTurn)
        {
            SelectScroll(DefaultScrollIndex);
        }

        void OnUpdatePlayerUnit(BaseUnit unit)
        {
            playerHpBar.SetGuage(unit.originStat.hp, unit.nowStat.hp);
        }

        void OnUpdateEnemyHp(List<BaseUnit> unitList)
        {
            foreach (var unit in unitList)
            {
                if (enemyHpBarDic.ContainsKey(unit))
                {
                    enemyHpBarDic[unit].SetGuage(unit.originStat.hp, unit.nowStat.hp);
                }
            }
        }

        void OnSpawnPlayerUnit(BaseUnit unit)
        {

        }

        void OnSpawnEnemyUnits(List<BaseUnit> enemyList)
        {
            enemyHpBarDic.Clear();

            for (int i = 0; i < enemyHpBarList.Count; i++)
            {
                enemyHpBarList[i].gameObject.SetActive(false);
                enemyTargetButton[i].onClick.RemoveAllListeners();
                enemyTargetButton[i].gameObject.SetActive(false);
            }

            for (int i = 0; i < enemyList.Count; i++)
            {
                var enemy = enemyList[i];
                var hpBar = enemyHpBarList[i];
                var button = enemyTargetButton[i];
                enemyHpBarDic.Add(enemy, hpBar);
                hpBar.gameObject.SetActive(true);
                button.onClick.AddListener(() => selectedTarget = enemy);
                button.gameObject.SetActive(true);
            }
        }
    }
}
