using System.Collections.Generic;
using UnityEngine;

namespace AutoBattler.Battle
{
    /// <summary>
    /// 전투 그리드. 너비 5, 높이 10.
    /// 아군 배치 영역: y = 0..3 (5x4)
    /// 적군 배치 영역: y = 6..9 (관례, 데이터에서 자유 배치)
    /// 좌표는 (x, y) — x: 0~Width-1, y: 0~Height-1.
    /// 거리 계산은 맨해튼 거리(4방향, 대각 금지)를 사용.
    ///   대각 이동을 허용하려면 Distance를 ChebyshevDistance로 교체하고
    ///   NextStepToward의 _dirs4 를 8방향 배열로 바꾸면 됨.
    /// </summary>
    public class BattleGrid
    {
        public const int Width = 5;
        public const int Height = 10;
        public const int AllyZoneMaxY = 3; // 0..3 = 아군 배치 가능

        // 셀 점유자: null 이면 비어있음
        private readonly BattleUnit[,] _occupants = new BattleUnit[Width, Height];

        public bool InBounds(Vector2Int p) =>
            p.x >= 0 && p.x < Width && p.y >= 0 && p.y < Height;

        public bool IsAllyZone(Vector2Int p) => p.y <= AllyZoneMaxY;

        public BattleUnit GetOccupant(Vector2Int p) =>
            InBounds(p) ? _occupants[p.x, p.y] : null;

        public bool IsEmpty(Vector2Int p) =>
            InBounds(p) && _occupants[p.x, p.y] == null;

        public bool TryPlace(BattleUnit unit, Vector2Int p)
        {
            if (!IsEmpty(p)) return false;
            _occupants[p.x, p.y] = unit;
            unit.Cell = p;
            return true;
        }

        public void Remove(BattleUnit unit)
        {
            var p = unit.Cell;
            if (InBounds(p) && _occupants[p.x, p.y] == unit)
                _occupants[p.x, p.y] = null;
        }

        public bool TryMove(BattleUnit unit, Vector2Int dest)
        {
            if (!IsEmpty(dest)) return false;
            Remove(unit);
            return TryPlace(unit, dest);
        }

        /// <summary>맨해튼 거리 (4방향, 대각 = 2). 이동/탐색용 기본 거리.</summary>
        public static int Distance(Vector2Int a, Vector2Int b) =>
            Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        /// <summary>체비셰프 거리 (8방향, 대각 = 1). 원거리/범위 공격 사거리용.</summary>
        public static int ChebyshevDistance(Vector2Int a, Vector2Int b) =>
            Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));

        /// <summary>
        /// 공격 사거리 판정.
        ///   range == 1 (근접): 맨해튼 — 대각은 거리 2가 되어 자동 제외
        ///   range >= 2 (원거리/범위): 체비셰프 — 대각도 인접으로 취급
        /// </summary>
        public static bool InAttackRange(Vector2Int self, Vector2Int target, int range)
        {
            if (range <= 1)
                return Distance(self, target) <= range;
            return ChebyshevDistance(self, target) <= range;
        }

        /// <summary>
        /// 가장 가까운 적 반환. 동률이면 후보 중 랜덤.
        /// </summary>
        public BattleUnit FindNearestEnemy(BattleUnit self, List<BattleUnit> allUnits)
        {
            int best = int.MaxValue;
            var tied = new List<BattleUnit>();
            foreach (var u in allUnits)
            {
                if (u == null || !u.IsAlive) continue;
                if (u.Team == self.Team) continue;
                int d = Distance(self.Cell, u.Cell);
                if (d < best) { best = d; tied.Clear(); tied.Add(u); }
                else if (d == best) tied.Add(u);
            }
            return tied.Count == 0 ? null : tied[Random.Range(0, tied.Count)];
        }

        // 4방향(상/하/좌/우)
        private static readonly Vector2Int[] _dirs4 = new Vector2Int[]
        {
            new Vector2Int( 1,  0),
            new Vector2Int(-1,  0),
            new Vector2Int( 0,  1),
            new Vector2Int( 0, -1),
        };

        /// <summary>
        /// self 에서 target 으로 1칸 다가가는 다음 위치.
        /// 4방향(상/하/좌/우)만 허용. 점유된 칸은 자동 제외.
        ///
        /// 우선순위:
        ///   1. 장축(타깃과 차이가 큰 축) 방향 직진 — 갈지자 방지
        ///   2. 장축이 막혀있으면 단축 방향으로 우회
        ///   3. 같은 축 안에서도 막혀있으면 다른 옵션
        /// </summary>
        public Vector2Int? NextStepToward(BattleUnit self, Vector2Int target)
        {
            int dx = target.x - self.Cell.x;
            int dy = target.y - self.Cell.y;

            // 각 축 방향 단위 벡터 (0이면 이미 그 축은 일치)
            int sx = dx == 0 ? 0 : (dx > 0 ? 1 : -1);
            int sy = dy == 0 ? 0 : (dy > 0 ? 1 : -1);

            // 이미 도착했으면 null
            if (sx == 0 && sy == 0) return null;

            bool yIsLong = Mathf.Abs(dy) > Mathf.Abs(dx);

            Vector2Int longStep = yIsLong ? new Vector2Int(0, sy) : new Vector2Int(sx, 0);
            Vector2Int shortStep = yIsLong ? new Vector2Int(sx, 0) : new Vector2Int(0, sy);

            // 1순위: 장축 직진
            var step = TryStep(self.Cell, longStep);
            if (step.HasValue) return step;

            // 2순위: 단축 방향. 이미 정렬된 경우(shortStep=zero)엔 좌우 어느 쪽이든 시도해서 우회.
            if (shortStep != Vector2Int.zero)
            {
                step = TryStep(self.Cell, shortStep);
                if (step.HasValue) return step;
            }
            else
            {
                // 같은 축에 정렬되어 직진 막힘 — 좌우(또는 상하) 어느 쪽이든 빈 칸으로 우회
                Vector2Int sideA = yIsLong ? new Vector2Int(1, 0) : new Vector2Int(0, 1);
                Vector2Int sideB = yIsLong ? new Vector2Int(-1, 0) : new Vector2Int(0, -1);
                // 매번 같은 쪽으로 쏠리지 않게 랜덤 우선순위
                if (Random.value < 0.5f) { var tmp = sideA; sideA = sideB; sideB = tmp; }
                step = TryStep(self.Cell, sideA);
                if (step.HasValue) return step;
                step = TryStep(self.Cell, sideB);
                if (step.HasValue) return step;
            }

            // 3순위: 그래도 갈 데 없으면 거리 줄어드는 어떤 빈 칸이든
            int curDist = Distance(self.Cell, target);
            var fallback = new List<Vector2Int>(4);
            for (int i = 0; i < _dirs4.Length; i++)
            {
                var next = self.Cell + _dirs4[i];
                if (!InBounds(next) || !IsEmpty(next)) continue;
                if (Distance(next, target) < curDist) fallback.Add(next);
            }
            if (fallback.Count > 0) return fallback[Random.Range(0, fallback.Count)];

            return null;
        }

        /// <summary>방향 한 칸 시도. 빈 칸이면 그 위치 반환.</summary>
        private Vector2Int? TryStep(Vector2Int from, Vector2Int dir)
        {
            if (dir == Vector2Int.zero) return null;
            var next = from + dir;
            if (!InBounds(next) || !IsEmpty(next)) return null;
            return next;
        }
    }
}