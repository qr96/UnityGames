using System;
using System.Collections.Generic;
using UnityEngine;

namespace AutoBattler.Battle.FX
{
    /// <summary>
    /// 투사체 매니저. 프리팹별로 풀 분리 (같은 프리팹은 재사용, 다른 프리팹은 별도 풀).
    /// SkillExecutor가 SpawnTowardUnit / SpawnTowardPoint 호출.
    /// </summary>
    public class ProjectileManager : MonoBehaviour
    {
        public static ProjectileManager Instance { get; private set; }

        [Header("연결")]
        public Transform container;  // 자식으로 둘 곳. 비우면 자기 자신

        // 프리팹별 풀
        private readonly Dictionary<GameObject, Queue<Projectile>> _pools
            = new Dictionary<GameObject, Queue<Projectile>>();

        // 인스턴스 → 어떤 풀로 돌려보낼지
        private readonly Dictionary<Projectile, GameObject> _origin
            = new Dictionary<Projectile, GameObject>();

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
        public void SpawnTowardUnit(GameObject prefab, Vector3 from, BattleUnit target,
                                    float speed, Action onArrive)
        {
            if (prefab == null) { onArrive?.Invoke(); return; }
            var p = Rent(prefab);
            p.LaunchToUnit(from, target, speed, onArrive);
        }

        public void SpawnTowardPoint(GameObject prefab, Vector3 from, Vector3 to,
                                     float speed, Action onArrive)
        {
            if (prefab == null) { onArrive?.Invoke(); return; }
            var p = Rent(prefab);
            p.LaunchToPoint(from, to, speed, onArrive);
        }

        public void Recycle(Projectile p)
        {
            if (p == null) return;
            p.gameObject.SetActive(false);

            if (_origin.TryGetValue(p, out var prefab))
            {
                if (!_pools.TryGetValue(prefab, out var q))
                {
                    q = new Queue<Projectile>();
                    _pools[prefab] = q;
                }
                q.Enqueue(p);
            }
            else
            {
                // 원본 추적 못 함 → 그냥 파괴
                Destroy(p.gameObject);
            }
        }

        // ─────────────────────────────────────────────────────────
        private Projectile Rent(GameObject prefab)
        {
            // 해당 프리팹의 풀에서 꺼냄
            if (_pools.TryGetValue(prefab, out var q) && q.Count > 0)
            {
                var existing = q.Dequeue();
                if (existing != null) return existing;
            }

            // 새로 만듦
            var parent = container != null ? container : transform;
            var go = Instantiate(prefab, parent);
            var p = go.GetComponent<Projectile>();
            if (p == null) p = go.AddComponent<Projectile>();
            _origin[p] = prefab;
            return p;
        }
    }
}