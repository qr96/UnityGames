using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GameUI
{
    public class HudLayout : MonoBehaviour
    {
        Camera mainCamera;
        RectTransform rectTransform;

        public TMP_Text damagePrefab;

        Stack<TMP_Text> damagePool = new Stack<TMP_Text>();

        readonly float damageTextSpace = 36f;

        private void OnEnable()
        {
            mainCamera = Camera.main;
            rectTransform = GetComponent<RectTransform>();
        }

        public void ShowDamage(long[] damages, Vector3 position)
        {
            var rectPos = WorldToAnchored(position, rectTransform);

            for (int i = damages.Length - 1; i >= 0; i--)
                ShowDamageTween(damages[i], rectPos + new Vector2(0f, damageTextSpace * i));
        }

        void ShowDamageTween(long damage, Vector2 startPosition)
        {
            var damageIns = damagePool.Count > 0 ? damagePool.Pop() : Instantiate(damagePrefab, transform);

            damageIns.gameObject.SetActive(true);
            damageIns.text = damage.ToString();
            damageIns.alpha = 1f;
            damageIns.transform.SetAsLastSibling();
            damageIns.rectTransform.anchoredPosition = startPosition;
            damageIns.rectTransform.DOAnchorPosY(startPosition.y + 50f, 0.5f);
            damageIns.DOFade(0f, 0.5f)
                .SetEase(Ease.InCirc)
                .OnComplete(() => damagePool.Push(damageIns));
        }

        Vector2 WorldToAnchored(Vector3 worldPos, RectTransform parentRect)
        {
            var screenPoint = mainCamera.WorldToScreenPoint(worldPos);

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, null, out var anchoredPos))
                return anchoredPos;

            return Vector2.zero;
        }
    }
}
