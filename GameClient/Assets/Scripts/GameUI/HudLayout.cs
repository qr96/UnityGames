using DG.Tweening;
using InGame;
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
        public TMP_Text namePrefab;

        public int maxHpGuageCount;

        Camera mainCamera;
        RectTransform rectTransform;

        Stack<TMP_Text> damagePool = new Stack<TMP_Text>();
        
        Dictionary<Transform, GuageBar> hpRegisterDic = new Dictionary<Transform, GuageBar>();
        LinkedList<Transform> hpRegisterQue = new LinkedList<Transform>();
        Stack<GuageBar> inactiveHpPool = new Stack<GuageBar>();

        Dictionary<Transform, TMP_Text> nameRegisterDic = new Dictionary<Transform, TMP_Text>();
        Stack<TMP_Text> namePool = new Stack<TMP_Text>();

        readonly float damageTextSpace = 36f;

        private void Awake()
        {
            Debug.Log("Awake");
        }

        private void OnEnable()
        {
            mainCamera = Camera.main;
            rectTransform = GetComponent<RectTransform>();
        }

        private void LateUpdate()
        {
            UpdateHpGuagePos();
            UpdateNameTagPos();
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

            if (hpGuage != null)
            {
                hpGuage.SetActive(true);
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

        public void RegisterNameTag(string name, Transform transform)
        {
            if (!nameRegisterDic.ContainsKey(transform))
            {
                var nameTag = namePool.Count > 0 ? namePool.Pop() : Instantiate(namePrefab, namePrefab.transform.parent);
                nameTag.text = name;
                nameTag.gameObject.SetActive(true);

                nameRegisterDic.Add(transform, nameTag);
            }
        }

        public void RemoveNameTag(Transform transform)
        {
            if (nameRegisterDic.ContainsKey(transform))
            {
                var nameTag = nameRegisterDic[transform];
                if (nameTag != null)
                    nameTag.gameObject.SetActive(false);
                nameRegisterDic.Remove(transform);
                namePool.Push(nameTag);
            }
        }

        void UpdateHpGuagePos()
        { 
            foreach (var register in hpRegisterQue)
            {
                var hpGuage = hpRegisterDic[register];
                var rectPos = WorldToAnchored(register.position, rectTransform);
                hpGuage.rectTransform.anchoredPosition = rectPos + new Vector2(0f, 80f);
            }
        }

        void UpdateNameTagPos()
        {
            foreach (var name in nameRegisterDic)
            {
                var anchoredPos = WorldToAnchored(name.Key.position, rectTransform);
                name.Value.rectTransform.anchoredPosition = anchoredPos + new Vector2(0f, 106f);
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
