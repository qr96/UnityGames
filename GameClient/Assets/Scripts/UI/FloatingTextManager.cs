using System.Collections.Generic;
using UnityEngine;

namespace AutoBattler.Battle.FX
{
    /// <summary>
    /// 떠오르는 텍스트 매니저. 씬에 GameObject 하나 두고 인스펙터 연결.
    ///
    /// 사용:
    ///   FloatingTextManager.Instance.SpawnDamage(worldPos, 15, isCrit: false);
    ///   FloatingTextManager.Instance.SpawnHeal(worldPos, 10);
    /// </summary>
    public class FloatingTextManager : MonoBehaviour
    {
        public static FloatingTextManager Instance { get; private set; }

        [Header("연결")]
        public FloatingText prefab;              // 자식에 TMP_Text 있는 World Space 텍스트 프리팹
        public Transform container;            // 자식으로 둘 컨테이너 (World Space Canvas 권장)
        public Camera lookAtCamera;         // 마주볼 카메라. 비우면 Camera.main

        [Header("스타일")]
        public Color damageColor = new Color(1f, 1f, 1f);          // 흰
        public Color critColor = new Color(1f, 0.7f, 0.1f);      // 황금
        public Color healColor = new Color(0.4f, 1f, 0.4f);      // 초록
        public float damageScale = 1f;
        public float critScale = 1.5f;

        [Header("스폰 위치 보정")]
        public float yOffset = 1.2f;             // 유닛 머리 위쪽

        private readonly Queue<FloatingText> _pool = new Queue<FloatingText>();
        private readonly List<FloatingText> _active = new List<FloatingText>();

        // ─────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (lookAtCamera == null) lookAtCamera = Camera.main;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void LateUpdate()
        {
            if (lookAtCamera == null) return;
            // 활성 텍스트들이 카메라 마주보게
            for (int i = 0; i < _active.Count; i++)
                _active[i]?.FaceCamera(lookAtCamera);
        }

        // ─────────────────────────────────────────────────────────
        // 외부 API
        // ─────────────────────────────────────────────────────────
        public void SpawnDamage(Vector3 worldPos, float amount, bool isCrit)
        {
            string text = $"-{Mathf.CeilToInt(amount)}";
            var color = isCrit ? critColor : damageColor;
            var scale = isCrit ? critScale : damageScale;
            Spawn(worldPos, text, color, scale);
        }

        public void SpawnHeal(Vector3 worldPos, float amount)
        {
            Spawn(worldPos, $"+{Mathf.CeilToInt(amount)}", healColor, damageScale);
        }

        public void Spawn(Vector3 worldPos, string text, Color color, float scale = 1f)
        {
            if (prefab == null) return;

            FloatingText ft;
            if (_pool.Count > 0)
            {
                ft = _pool.Dequeue();
            }
            else
            {
                ft = Instantiate(prefab, container != null ? container : transform);
            }

            Vector3 pos = worldPos + Vector3.up * yOffset;
            ft.Show(pos, text, color, scale);
            ft.FaceCamera(lookAtCamera);
            if (!_active.Contains(ft)) _active.Add(ft);
        }

        public void Recycle(FloatingText ft)
        {
            if (ft == null) return;
            ft.gameObject.SetActive(false);
            _active.Remove(ft);
            _pool.Enqueue(ft);
        }
    }
}