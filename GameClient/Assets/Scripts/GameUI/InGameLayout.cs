using InGame;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public class InGameLayout : MonoBehaviour
    {
        public List<ElementSlot> elementSlots = new List<ElementSlot>();
        public Button rollButton;
        public Dictionary<string, Sprite> spritesDic = new Dictionary<string, Sprite>();

        private void Awake()
        {
            LoadSprites();

            GameStage.Instance.OnChangeElementSlots += SetElementSlots;
            GameStage.Instance.OnChangeLock += (index, isLocked) => elementSlots[index].SetLockImage(isLocked);

            rollButton.onClick.AddListener(() => GameStage.Instance.RollDice());
            for (int i = 0; i < Common.MaxSlotCount; i++)
            {
                var slot = elementSlots[i];
                var index = i;
                slot.button.onClick.AddListener(() => GameStage.Instance.ChangeSlotLock(index));
            }
        }

        private void LoadSprites()
        {
            var elements = Resources.LoadAll<Sprite>("UI/Elements000");
            foreach (var element in elements)
            {
                Debug.Log(element.name);
                spritesDic.Add(element.name, element);
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
            if (element == Common.ElementType.None)
                return $"Elements000_0";
            return $"Elements000_{(int)element - 1}";
        }
    }
}
