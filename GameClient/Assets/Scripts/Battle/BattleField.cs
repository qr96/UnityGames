using System;
using System.Collections.Generic;
using UnityEngine;
using AutoBattler.Core;
using AutoBattler.Data;
using AutoBattler.Heroes;

namespace AutoBattler.Battle
{
    /// <summary>
    /// 한 판의 전투를 관리. 그리드/유닛/상태머신 보유.
    /// 외부에서 StartBattle()로 시작, OnBattleEnded 이벤트로 결과 통지.
    /// </summary>
    public class BattleField : MonoBehaviour
    {
        [Header("렌더링 (3D, XZ 평면)")]
        // x = 좌우 칸 간격(월드 X), z = 앞뒤 칸 간격(월드 Z)
        public Vector2 cellSize = new Vector2(1.1f, 1.1f);
        public float groundY = 0f; // 유닛이 서는 바닥 높이
        public Transform unitsRoot;

        [Header("풀링 (PoolManager)")]
        [Tooltip("PoolManagerConfig에 등록된 Resources 경로. 비워두면 unitPrefabFallback 사용")]
        public string defaultUnitPoolKey = "Units/Unit_Base";
        [Tooltip("PoolManager가 없거나 풀에 없을 때 폴백으로 Instantiate")]
        public GameObject unitPrefabFallback;

        public BattleGrid Grid { get; private set; }
        public BattleState State { get; private set; } = BattleState.Idle;

        public event Action<bool> OnBattleEnded; // true=승, false=패

        private readonly List<BattleUnit> _units = new List<BattleUnit>();
        private bool _warnedNoUnitsRoot;

        // ─────────────────────────────────────────────────────────
        /// <summary>
        /// 전투 시작. heroes의 배치는 placement에서 가져옴.
        /// placement에 없는 영웅은 자동으로 빈 칸에 배치.
        /// </summary>
        public void StartBattle(List<Hero> heroes, Dictionary<Hero, Vector2Int> placement,
                                List<EnemySpawn> enemies)
        {
            CleanUp();
            Grid = new BattleGrid();
            State = BattleState.Preparing;

            // 아군 배치
            SpawnHeroesWithPlacement(heroes, placement);

            // 적군: 데이터로 받은 위치에 배치
            foreach (var e in enemies) SpawnEnemy(e);

            State = BattleState.Running;

            UnityEngine.Debug.Log($"[BattleField] StartBattle 완료. _units={_units.Count}");
        }

        /// <summary>
        /// 전투 시작 전 영웅과 적을 그리드에 올려두는 "미리보기" 모드.
        /// State는 Preparing이라 AI 안 돎. 배치 화면에서 사용.
        ///
        /// previewEnemies가 있으면 적도 함께 표시. 없으면 영웅만.
        /// </summary>
        public void EnterPlacementPreview(List<Hero> heroes,
                                          Dictionary<Hero, Vector2Int> placement,
                                          List<EnemySpawn> previewEnemies = null)
        {
            CleanUp();
            Grid = new BattleGrid();
            State = BattleState.Preparing;

            SpawnHeroesWithPlacement(heroes, placement);

            if (previewEnemies != null)
                foreach (var e in previewEnemies) SpawnEnemy(e);

            // State는 Preparing 유지 — Update의 AI Tick은 안 돔
        }

        public void ExitPlacementPreview()
        {
            CleanUp();
        }

        /// <summary>현재 미리보기에 떠있는 영웅 BattleUnit을 셀로 찾기. 드래그 픽킹용.</summary>
        public BattleUnit GetUnitAt(Vector2Int cell)
        {
            return Grid?.GetOccupant(cell);
        }

        /// <summary>현재 살아있는 모든 아군 BattleUnit 순회 (배치 화면용).</summary>
        public IEnumerable<BattleUnit> AllAllyUnits()
        {
            foreach (var u in _units)
                if (u != null && u.Team == Team.Ally && u.IsAlive) yield return u;
        }

        private void SpawnHeroesWithPlacement(List<Hero> heroes,
                                              Dictionary<Hero, Vector2Int> placement)
        {
            // 우선순위 1: placement에 있는 영웅
            // 우선순위 2: placement에 없는 영웅 → 빈 칸 앞에서부터
            var unplaced = new List<Hero>();

            foreach (var hero in heroes)
            {
                if (placement != null && placement.TryGetValue(hero, out var cell))
                {
                    if (Grid.IsEmpty(cell) && Grid.IsAllyZone(cell))
                        SpawnHero(hero, cell);
                    else
                        unplaced.Add(hero); // 충돌이나 잘못된 셀이면 자동 배치로
                }
                else unplaced.Add(hero);
            }

            // 자동 배치 폴백
            foreach (var hero in unplaced)
            {
                bool placed = false;
                for (int y = 0; y <= BattleGrid.AllyZoneMaxY && !placed; y++)
                    for (int x = 0; x < BattleGrid.Width && !placed; x++)
                    {
                        var c = new Vector2Int(x, y);
                        if (Grid.IsEmpty(c)) { SpawnHero(hero, c); placed = true; }
                    }
            }
        }

        public void CleanUp()
        {
            foreach (var u in _units)
            {
                if (u == null) continue;

                // 이미 풀에 반납된 유닛(=비활성 상태)은 건너뜀.
                // 사망 처리(OnDeath → HideAfterDeath)가 ReleaseSelf 호출했을 수 있음.
                if (!u.gameObject.activeSelf) continue;

                ReturnUnit(u.gameObject);
            }
            _units.Clear();
            State = BattleState.Idle;
        }

        // ─────────────────────────────────────────────────────────
        // 유닛 획득 / 반납 — PoolManager 우선, 없으면 Instantiate 폴백
        // ─────────────────────────────────────────────────────────
        private GameObject AcquireUnit()
        {
            // unitsRoot 미연결 시 경고 (한 번만)
            if (unitsRoot == null && !_warnedNoUnitsRoot)
            {
                UnityEngine.Debug.LogWarning(
                    "[BattleField] unitsRoot가 연결되지 않았습니다. " +
                    "유닛 정리/배치를 위해 인스펙터에서 unitsRoot 슬롯에 빈 GameObject를 연결하세요. " +
                    "임시로 BattleField 자신을 부모로 사용합니다.");
                _warnedNoUnitsRoot = true;
            }
            Transform parent = unitsRoot != null ? unitsRoot : transform;

            // PoolManager 가 있고 키가 지정되어 있으면 풀에서
            if (PoolManager.Instance != null && !string.IsNullOrEmpty(defaultUnitPoolKey))
            {
                if (PoolManager.Instance.TryCreate(defaultUnitPoolKey, out var pooled))
                {
                    pooled.transform.SetParent(parent, false);
                    return pooled;
                }
            }
            // 폴백
            if (unitPrefabFallback == null)
            {
                Debug.LogError("[BattleField] PoolManager도 없고 unitPrefabFallback도 비어있습니다.");
                return null;
            }
            return Instantiate(unitPrefabFallback, parent);
        }

        private void ReturnUnit(GameObject go)
        {
            if (go == null) return;

            // 이미 비활성(=풀에 반납됨) 이면 무시. 이중 Release 방지.
            if (!go.activeSelf) return;

            // Poolable 이 붙어있으면 풀로, 아니면 그냥 Destroy
            var poolable = go.GetComponent<Poolable>();
            if (poolable != null && PoolManager.Instance != null)
                poolable.ReleaseSelf();
            else
                Destroy(go);
        }

        // ─────────────────────────────────────────────────────────
        private BattleUnit SpawnHero(Hero hero, Vector2Int cell)
        {
            var go = AcquireUnit();
            if (go == null) return null;

            go.name = $"Hero_{hero.data.displayName}";
            var u = go.GetComponent<BattleUnit>();
            if (u == null)
            {
                UnityEngine.Debug.LogError(
                    $"[BattleField] 풀에서 꺼낸 '{go.name}'에 BattleUnit 컴포넌트가 없습니다. " +
                    $"프리팹 (Resources/{defaultUnitPoolKey})에 BattleUnit 스크립트를 추가하세요.");
                ReturnUnit(go);
                return null;
            }
            u.InitAsHero(hero, this, Team.Ally);
            Grid.TryPlace(u, cell);
            u.transform.position = CellToWorld(cell);
            u.transform.rotation = Quaternion.LookRotation(Vector3.forward); // 적 진영 방향
            u.DesiredFacing = Vector3.forward;
            _units.Add(u);
            return u;
        }

        private BattleUnit SpawnEnemy(EnemySpawn spawn)
        {
            var go = AcquireUnit();
            if (go == null) return null;

            go.name = $"Enemy_{spawn.name}";
            var u = go.GetComponent<BattleUnit>();
            if (u == null)
            {
                UnityEngine.Debug.LogError(
                    $"[BattleField] 풀에서 꺼낸 '{go.name}'에 BattleUnit 컴포넌트가 없습니다. " +
                    $"프리팹 (Resources/{defaultUnitPoolKey})에 BattleUnit 스크립트를 추가하세요.");
                ReturnUnit(go);
                return null;
            }
            u.InitAsEnemy(spawn.name, spawn.stats, spawn.weapon, spawn.skills, this);
            Grid.TryPlace(u, spawn.cell);
            u.transform.position = CellToWorld(spawn.cell);
            u.transform.rotation = Quaternion.LookRotation(Vector3.back);    // 아군 진영 방향
            u.DesiredFacing = Vector3.back;
            _units.Add(u);
            return u;
        }

        // ─────────────────────────────────────────────────────────
        // 시간 진행
        // ─────────────────────────────────────────────────────────
        [Header("시각 보간")]
        public float turnLerpSpeed = 12f;   // 회전 따라가는 속도

        private void Update()
        {
            if (State != BattleState.Running) return;

            float dt = Time.deltaTime;

            // AI/이동 Tick — 매 프레임 호출 (BattleUnit이 직접 부드러운 보간 처리)
            foreach (var u in _units)
                if (u != null && u.IsAlive)
                    u.Tick(dt, _units);

            CheckVictory();
            if (State != BattleState.Running) return;

            // 회전만 부드럽게 따라감 (위치는 BattleUnit이 직접 갱신)
            foreach (var u in _units)
            {
                if (u == null || !u.IsAlive) continue;

                if (u.DesiredFacing.sqrMagnitude > 0.0001f)
                {
                    var look = Quaternion.LookRotation(u.DesiredFacing, Vector3.up);
                    u.transform.rotation = Quaternion.Slerp(
                        u.transform.rotation, look, 1f - Mathf.Exp(-turnLerpSpeed * dt));
                }
            }
        }

        private void CheckVictory()
        {
            bool anyAlly = false, anyEnemy = false;
            foreach (var u in _units)
            {
                if (u == null || !u.IsAlive) continue;
                if (u.Team == Team.Ally) anyAlly = true;
                else anyEnemy = true;
                if (anyAlly && anyEnemy) return;
            }

            if (!anyEnemy) { State = BattleState.Won; OnBattleEnded?.Invoke(true); }
            else if (!anyAlly) { State = BattleState.Lost; OnBattleEnded?.Invoke(false); }
        }

        // ─────────────────────────────────────────────────────────
        /// <summary>
        /// 셀 좌표(x,y)를 3D 월드 좌표로 변환.
        ///   - 월드 X = 그리드 x축 (좌우)
        ///   - 월드 Z = 그리드 y축 (앞뒤, 아군 쪽이 -Z, 적군 쪽이 +Z)
        ///   - 월드 Y = groundY (바닥 고정)
        /// 그리드 중앙이 원점에 오도록 정렬.
        /// </summary>
        public Vector3 CellToWorld(Vector2Int c)
        {
            float x = (c.x - (BattleGrid.Width - 1) * 0.5f) * cellSize.x;
            float z = (c.y - (BattleGrid.Height - 1) * 0.5f) * cellSize.y;
            return new Vector3(x, groundY, z);
        }

        /// <summary>월드 좌표를 가장 가까운 셀로 스냅. 범위 밖이면 클램프.</summary>
        public Vector2Int WorldToCell(Vector3 world)
        {
            int x = Mathf.RoundToInt(world.x / cellSize.x + (BattleGrid.Width - 1) * 0.5f);
            int y = Mathf.RoundToInt(world.z / cellSize.y + (BattleGrid.Height - 1) * 0.5f);
            x = Mathf.Clamp(x, 0, BattleGrid.Width - 1);
            y = Mathf.Clamp(y, 0, BattleGrid.Height - 1);
            return new Vector2Int(x, y);
        }
    }

    [Serializable]
    public class EnemySpawn
    {
        public string name;
        public Vector2Int cell;
        public Stats stats;
        public WeaponData weapon;
        public List<SkillData> skills;
    }
}