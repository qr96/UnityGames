using UnityEngine;
using TMPro;

namespace AutoBattler.Battle.FX
{
    /// <summary>
    /// 떠오르는 텍스트. World Space Canvas 자식으로 사용.
    /// FloatingTextManager가 풀링.
    ///
    /// 사용:
    ///   var ft = FloatingTextManager.Instance.Spawn(worldPos, "-15", Color.white);
    ///   FloatingTextManager.Instance.Spawn(worldPos, "+10", Color.green);
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("애니메이션")]
        public float lifetime = 1.0f;
        public float riseSpeed = 1.5f;       // 월드 m/s
        public Vector2 horizontalJitter = new Vector2(0.2f, 0.2f); // X,Z 흔들림

        // 상태
        private float _elapsed;
        private Vector3 _velocity;

        // ─────────────────────────────────────────────────────────
        public void Show(Vector3 worldPos, string text, Color color, float scale = 1f)
        {
            transform.position = worldPos;
            transform.localScale = Vector3.one * scale;

            if (label != null)
            {
                label.text = text;
                label.color = color;
            }
            if (canvasGroup != null) canvasGroup.alpha = 1f;

            // 약간 좌우 흔들림 + 위로 떠오름
            _velocity = new Vector3(
                Random.Range(-horizontalJitter.x, horizontalJitter.x),
                riseSpeed,
                Random.Range(-horizontalJitter.y, horizontalJitter.y));

            _elapsed = 0f;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = _elapsed / lifetime;

            transform.position += _velocity * Time.deltaTime;
            if (canvasGroup != null) canvasGroup.alpha = 1f - Mathf.Clamp01(t);

            if (t >= 1f)
            {
                FloatingTextManager.Instance?.Recycle(this);
            }
        }

        /// <summary>월드 좌표 기준 카메라 마주보기. Manager가 매니징 외부에서 호출 가능.</summary>
        public void FaceCamera(Camera cam)
        {
            if (cam == null) return;
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }
    }
}