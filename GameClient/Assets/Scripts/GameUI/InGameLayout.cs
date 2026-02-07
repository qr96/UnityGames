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
        public GuageBar enemyHpBar;

        int selectedScrollIndex;

        private void Awake()
        {
            scrollPrefab.gameObject.SetActive(false);

            LoadSprites();
            
            GameStage.Instance.OnChangeElementSlots += SetElementSlots;
            GameStage.Instance.OnChangeLock += (index, isLocked) => elementSlots[index].SetLockImage(isLocked);
            GameStage.Instance.OnChangeScrollSlots += OnUpdateScrollSlots;
            GameStage.Instance.OnChangePlayerUnit += OnUpdatePlayerUnit;
            GameStage.Instance.OnChageEnemyUnit += OnUpdateEnemyHp;

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
            //if (element == Common.ElementType.None)
            //    return $"Elements000_0";
            //return $"Elements000_{(int)element - 1}";
            return $"Icons000_{(int)element}";
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
                selectedScrollIndex = -1;
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
                return;

            GameStage.Instance.UseScroll(selectedScrollIndex);
        }

        void OnUpdatePlayerUnit(BaseUnit unit)
        {
            playerHpBar.SetGuage(unit.originStat.hp, unit.nowStat.hp);
        }

        void OnUpdateEnemyHp(BaseUnit unit)
        {
            enemyHpBar.SetGuage(unit.originStat.hp, unit.nowStat.hp);
        }
    }
}
