using System.Collections.Generic;
using UnityEngine;

namespace AutoBattler.Battle.FX
{
    /// <summary>
    /// 유닛 피격/회복 깜빡임. BattleUnit에 부착하거나 자식 모델에 부착.
    /// MaterialPropertyBlock 사용 — 머티리얼 인스턴스 생성 안 함 (메모리/드로우콜 절약).
    ///
    /// 사용:
    ///   unitFlash.FlashHit();    // 빨강
    ///   unitFlash.FlashHeal();   // 초록
    /// </summary>
    public class UnitFlash : MonoBehaviour
    {
        [Header("자동 탐지 — 비워두면 자식의 모든 Renderer 찾음")]
        public Renderer[] renderers;

        [Header("색")]
        public Color hitColor = Color.red;
        public Color healColor = new Color(0.4f, 1f, 0.4f);

        [Header("시간")]
        public float flashDuration = 0.12f;

        // 내부
        private MaterialPropertyBlock _mpb;
        private static readonly int s_BaseColorID = Shader.PropertyToID("_BaseColor"); // URP
        private static readonly int s_ColorID = Shader.PropertyToID("_Color");     // BIRP

        private float _flashRemain;
        private Color _flashColor;
        private bool _hasBaseColor;
        private bool _hasColor;

        // 원래 색 (한 번만 캐시)
        private readonly List<Color> _originalBaseColors = new List<Color>();
        private readonly List<Color> _originalColors = new List<Color>();
        private bool _cached;

        // ─────────────────────────────────────────────────────────
        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
            CacheOriginalColors();
        }

        private void CacheOriginalColors()
        {
            if (_cached || renderers == null) return;
            _originalBaseColors.Clear();
            _originalColors.Clear();

            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null || r.sharedMaterial == null)
                {
                    _originalBaseColors.Add(Color.white);
                    _originalColors.Add(Color.white);
                    continue;
                }
                // 두 셰이더 모두 지원: URP의 _BaseColor 또는 Built-in의 _Color
                var mat = r.sharedMaterial;
                _hasBaseColor |= mat.HasProperty(s_BaseColorID);
                _hasColor |= mat.HasProperty(s_ColorID);

                _originalBaseColors.Add(mat.HasProperty(s_BaseColorID) ? mat.GetColor(s_BaseColorID) : Color.white);
                _originalColors.Add(mat.HasProperty(s_ColorID) ? mat.GetColor(s_ColorID) : Color.white);
            }
            _cached = true;
        }

        // ─────────────────────────────────────────────────────────
        public void FlashHit() => StartFlash(hitColor);
        public void FlashHeal() => StartFlash(healColor);

        private void StartFlash(Color c)
        {
            _flashColor = c;
            _flashRemain = flashDuration;
            ApplyColor(c);
        }

        private void Update()
        {
            if (_flashRemain <= 0f) return;
            _flashRemain -= Time.deltaTime;
            if (_flashRemain <= 0f) RestoreOriginal();
        }

        private void ApplyColor(Color c)
        {
            if (renderers == null) return;
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null) continue;
                r.GetPropertyBlock(_mpb);
                if (_hasBaseColor) _mpb.SetColor(s_BaseColorID, c);
                if (_hasColor) _mpb.SetColor(s_ColorID, c);
                r.SetPropertyBlock(_mpb);
            }
        }

        private void RestoreOriginal()
        {
            if (renderers == null) return;
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null) continue;
                r.GetPropertyBlock(_mpb);
                if (_hasBaseColor && i < _originalBaseColors.Count)
                    _mpb.SetColor(s_BaseColorID, _originalBaseColors[i]);
                if (_hasColor && i < _originalColors.Count)
                    _mpb.SetColor(s_ColorID, _originalColors[i]);
                r.SetPropertyBlock(_mpb);
            }
        }

        /// <summary>풀에서 재사용 시 색 강제 복원.</summary>
        public void ForceReset()
        {
            _flashRemain = 0f;
            RestoreOriginal();
        }
    }
}