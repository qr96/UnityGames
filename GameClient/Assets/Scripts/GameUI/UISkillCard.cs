using GameDefine;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameUI
{
    public class UISkillCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public UICardSlotManager manager;
        public TMP_Text info;
        public TMP_Text rank;
        public SkillCardData cardData;

        public RectTransform rectTransform;
        CanvasGroup canvasGroup;

        void Awake()
        {
            Debug.Log("Awake");
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            canvasGroup.blocksRaycasts = false; // 드래그 중 아래 UI 감지 허용
            canvasGroup.alpha = 0.6f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            rectTransform.position = eventData.position;
            manager.OnDrag(this);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;

            manager.OnEndDrag(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (cardData != null)
                manager.OnClick(cardData);
            else
                Debug.LogError($"OnPointerClick() cardData is null. name={gameObject.name}");
        }
    }
}
