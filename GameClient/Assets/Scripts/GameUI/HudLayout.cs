using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameUI
{
    public class HudLayout : MonoBehaviour
    {
        public TMP_Text damagePrefab;
        public GuageBar hpGuagePrefab;

        public int maxHpGuageCount = 5;

        Camera mainCamera;
        RectTransform rectTransform;

        Stack<TMP_Text> damagePool = new Stack<TMP_Text>();

        Dictionary<Transform, GuageBar> hpRegisterDic = new Dictionary<Transform, GuageBar>();
        LinkedList<Transform> hpRegisterQue = new LinkedList<Transform>();
        Stack<GuageBar> inactiveHpPool = new Stack<GuageBar>();

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

        public void RegisterHpGuage(Transform transform)
        {
            if (hpRegisterDic.ContainsKey(transform))
                return;

            if (hpRegisterQue.Count >= maxHpGuageCount)
                RemoveHpGuage(hpRegisterQue.First.Value);

            var hpGuage = inactiveHpPool.Count > 0 ? inactiveHpPool.Pop() : Instantiate(hpGuagePrefab, hpGuagePrefab.transform.parent);
            var rectPos = WorldToAnchored(transform.position, rectTransform);

            if (hpGuage != null)
            {
                hpGuage.SetActive(true);
                hpGuage.rectTransform.anchoredPosition = rectPos + new Vector2(0f, 60f);
                hpRegisterQue.AddLast(transform);
                hpRegisterDic.Add(transform, hpGuage);
            }
        }

        public void RemoveHpGuage(Transform transform)
        {
            if (hpRegisterDic.ContainsKey(transform) && hpRegisterDic[transform] != null)
            {
                hpRegisterDic[transform].SetActive(false);
                inactiveHpPool.Push(hpRegisterDic[transform]);
                hpRegisterDic.Remove(transform);
                hpRegisterQue.Remove(transform);
            }
        }

        public void UpdateHpGuageValue(Transform transform, long max, long now)
        {
            if (hpRegisterDic.ContainsKey(transform))
            {
                hpRegisterDic[transform].SetGuage(max, now);
            }
        }

        void ShowDamageTween(long damage, Vector2 startPosition)
        {
            var damageIns = damagePool.Count > 0 ? damagePool.Pop() : Instantiate(damagePrefab, transform);

            damageIns.gameObject.SetActive(true);
            damageIns.text = damage > 0 ? damage.ToString() : "block";
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
