using UnityEngine;

namespace AutoBattler.Battle.FX
{
    /// <summary>
    /// 스킬 시각 효과 한 인스턴스. 풀링됨.
    /// 두 가지 모드:
    ///   - Circle: AOE 범위 원 표시 (LineRenderer로 그림)
    ///   - Line: 시전자 → 타깃 직선
    /// 페이드 아웃 후 매니저에 자동 반납.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class SkillVfx : MonoBehaviour
    {
        [Header("애니메이션")]
        public float lifetime = 0.5f;
        public AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [Header("원")]
        public int circleSegments = 32;
        public float circleLiftY = 0.05f;

        // 내부
        private LineRenderer _lr;
        private float _elapsed;
        private Color _baseColor;

        private void Awake()
        {
            _lr = GetComponent<LineRenderer>();
            _lr.useWorldSpace = true;
            _lr.numCornerVertices = 2;
        }

        // ─────────────────────────────────────────────────────────
        /// <summary>AOE 원. center 기준 radius 셀 크기.</summary>
        public void ShowCircle(Vector3 center, float radiusWorld, Color color, float width = 0.05f)
        {
            _baseColor = color;
            _lr.startWidth = width;
            _lr.endWidth = width;
            _lr.loop = true;
            _lr.positionCount = circleSegments;

            for (int i = 0; i < circleSegments; i++)
            {
                float angle = (i / (float)circleSegments) * Mathf.PI * 2f;
                _lr.SetPosition(i, center + new Vector3(
                    Mathf.Cos(angle) * radiusWorld,
                    circleLiftY,
                    Mathf.Sin(angle) * radiusWorld));
            }
            Begin();
        }

        /// <summary>직선. 시전자 → 타깃.</summary>
        public void ShowLine(Vector3 from, Vector3 to, Color color, float width = 0.08f)
        {
            _baseColor = color;
            _lr.startWidth = width;
            _lr.endWidth = width;
            _lr.loop = false;
            _lr.positionCount = 2;

            // 살짝 띄움
            from.y += 0.5f;
            to.y += 0.5f;
            _lr.SetPosition(0, from);
            _lr.SetPosition(1, to);
            Begin();
        }

        private void Begin()
        {
            _elapsed = 0f;
            gameObject.SetActive(true);
            ApplyAlpha(1f);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = _elapsed / lifetime;
            if (t >= 1f)
            {
                SkillVfxManager.Instance?.Recycle(this);
                return;
            }
            ApplyAlpha(alphaCurve.Evaluate(t));
        }

        private void ApplyAlpha(float a)
        {
            var c = _baseColor;
            c.a = a;
            _lr.startColor = c;
            _lr.endColor = c;
        }
    }
}