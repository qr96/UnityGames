using System;
using UnityEngine;

namespace AutoBattler.Battle.FX
{
    /// <summary>
    /// 투사체 한 인스턴스. ProjectileManager가 풀링.
    ///
    /// 두 가지 발사 모드:
    ///   - LaunchToUnit: 타깃 BattleUnit 추적 (살아있으면 위치 따라감)
    ///   - LaunchToPoint: 지점 고정 (AOE처럼 타깃 위치만 사용)
    ///
    /// 도착 시 onArrive 콜백 호출 → SkillExecutor가 데미지/AOE 처리.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        public float arriveDistance = 0.3f;  // 이 거리 안으로 들어오면 도착 처리
        public float maxLifetime = 5f;       // 안전장치

        // 상태
        private Vector3 _targetPoint;
        private BattleUnit _targetUnit;     // null이면 지점 고정
        private float _speed;
        private float _elapsed;
        private Action _onArrive;
        private bool _active;

        // ─────────────────────────────────────────────────────────
        public void LaunchToUnit(Vector3 from, BattleUnit target, float speed, Action onArrive)
        {
            transform.position = from + Vector3.up * 0.5f;
            _targetUnit = target;
            _targetPoint = target != null ? target.transform.position : from;
            _speed = speed;
            _onArrive = onArrive;
            _elapsed = 0f;
            _active = true;
            gameObject.SetActive(true);
        }

        public void LaunchToPoint(Vector3 from, Vector3 to, float speed, Action onArrive)
        {
            transform.position = from + Vector3.up * 0.5f;
            _targetUnit = null;
            _targetPoint = to;
            _speed = speed;
            _onArrive = onArrive;
            _elapsed = 0f;
            _active = true;
            gameObject.SetActive(true);
        }

        // ─────────────────────────────────────────────────────────
        private void Update()
        {
            if (!_active) return;

            _elapsed += Time.deltaTime;
            if (_elapsed > maxLifetime) { Finish(triggerCallback: false); return; }

            // 타깃 추적 (살아있을 때만)
            if (_targetUnit != null && _targetUnit.IsAlive)
                _targetPoint = _targetUnit.transform.position + Vector3.up * 0.5f;

            // 이동
            Vector3 dir = _targetPoint - transform.position;
            float dist = dir.magnitude;

            if (dist <= arriveDistance)
            {
                Finish(triggerCallback: true);
                return;
            }

            Vector3 step = dir.normalized * _speed * Time.deltaTime;
            if (step.magnitude >= dist) transform.position = _targetPoint;
            else transform.position += step;

            // 진행 방향 보기
            if (dir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(dir);
        }

        private void Finish(bool triggerCallback)
        {
            _active = false;
            if (triggerCallback) _onArrive?.Invoke();
            _onArrive = null;
            _targetUnit = null;
            ProjectileManager.Instance?.Recycle(this);
        }
    }
}