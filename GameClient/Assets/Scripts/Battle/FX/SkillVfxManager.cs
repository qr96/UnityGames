using System.Collections.Generic;
using UnityEngine;

namespace AutoBattler.Battle.FX
{
    /// <summary>
    /// 스킬 시각 효과 매니저. 원/직선 풀링.
    /// SkillExecutor가 Instance.ShowAoeCircle / ShowSkillLine 호출.
    /// </summary>
    public class SkillVfxManager : MonoBehaviour
    {
        public static SkillVfxManager Instance { get; private set; }

        [Header("프리팹")]
        public SkillVfx prefab;          // LineRenderer + SkillVfx 컴포넌트 가진 프리팹
        public Transform container;       // 자식으로 둘 곳

        [Header("색")]
        public Color aoeColor = new Color(1f, 0.3f, 0.2f, 0.9f);   // AOE 빨강
        public Color singleColor = new Color(1f, 0.9f, 0.2f, 0.9f);  // 단일 노랑
        public Color healColor = new Color(0.4f, 1f, 0.5f, 0.9f);   // 힐 초록

        // 셀 크기 — BattleField와 일치해야 정확. 기본 1.1
        public float cellWorldSize = 1.1f;

        private readonly Queue<SkillVfx> _pool = new Queue<SkillVfx>();
        private readonly List<SkillVfx> _active = new List<SkillVfx>();

        // ─────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ─────────────────────────────────────────────────────────
        /// <summary>AOE 범위 원. radiusInCells = SkillData.areaRadius.</summary>
        public void ShowAoeCircle(Vector3 center, int radiusInCells, bool isFriendly = false)
        {
            var vfx = Rent();
            float r = (radiusInCells + 0.5f) * cellWorldSize; // 셀 중심에서 외곽까지
            vfx.ShowCircle(center, r, isFriendly ? healColor : aoeColor);
        }

        /// <summary>단일/원거리 직선. 시전자 → 타깃.</summary>
        public void ShowSkillLine(Vector3 from, Vector3 to, bool isFriendly = false)
        {
            var vfx = Rent();
            vfx.ShowLine(from, to, isFriendly ? healColor : singleColor);
        }

        public void Recycle(SkillVfx vfx)
        {
            if (vfx == null) return;
            vfx.gameObject.SetActive(false);
            _active.Remove(vfx);
            _pool.Enqueue(vfx);
        }

        private SkillVfx Rent()
        {
            SkillVfx vfx;
            if (_pool.Count > 0) vfx = _pool.Dequeue();
            else vfx = Instantiate(prefab, container != null ? container : transform);
            if (!_active.Contains(vfx)) _active.Add(vfx);
            return vfx;
        }
    }
}