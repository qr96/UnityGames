using System.Collections.Generic;
using UnityEngine;

namespace AutoBattler.Battle.FX
{
    /// <summary>
    /// HP바 매니저. 공용 World Space Canvas 자식으로 HP바들을 풀링.
    /// 라운드 시작 시 모든 유닛에 HP바 할당, 종료 시 회수.
    /// </summary>
    public class UnitHpBarManager : MonoBehaviour
    {
        public static UnitHpBarManager Instance { get; private set; }

        [Header("매니저")]
        public BattleField battleField;

        [Header("Canvas / 프리팹")]
        public Transform container;
        public UnitHpBar prefab;

        [Header("카메라")]
        public Camera lookAtCamera;

        private readonly Queue<UnitHpBar> _pool = new Queue<UnitHpBar>();
        private readonly List<UnitHpBar> _active = new List<UnitHpBar>();

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

        private void OnEnable()
        {
            if (battleField != null)
            {
                battleField.OnBattleStarted += HandleBattleStarted;
                battleField.OnBattleEnded += HandleBattleEnded;
            }
        }

        private void OnDisable()
        {
            if (battleField != null)
            {
                battleField.OnBattleStarted -= HandleBattleStarted;
                battleField.OnBattleEnded -= HandleBattleEnded;
            }
        }

        private void HandleBattleStarted()
        {
            ReturnAll(); // 안전: 잔재 있으면 정리
            if (battleField == null) return;
            foreach (var u in battleField.GetAllUnits())
            {
                if (u == null || !u.IsAlive) continue;
                var bar = Rent();
                bar.Bind(u);
            }
        }

        private void HandleBattleEnded(bool won)
        {
            ReturnAll();
        }

        private void LateUpdate()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var bar = _active[i];
                if (bar == null) { _active.RemoveAt(i); continue; }
                bar.FollowTarget(lookAtCamera);
            }
        }

        private UnitHpBar Rent()
        {
            UnitHpBar bar;
            if (_pool.Count > 0) bar = _pool.Dequeue();
            else bar = Instantiate(prefab, container);
            _active.Add(bar);
            return bar;
        }

        private void ReturnAll()
        {
            foreach (var bar in _active)
            {
                if (bar == null) continue;
                bar.Unbind();
                _pool.Enqueue(bar);
            }
            _active.Clear();
        }
    }
}