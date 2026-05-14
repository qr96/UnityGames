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

        /// <summary>맨해튼 거리 (4방향, 대각 = 2). 기본 거리 함수.</summary>
        public static int Distance(Vector2Int a, Vector2Int b) =>
            Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        /// <summary>체비셰프 거리 (8방향, 대각 = 1). 필요 시 사용.</summary>
        public static int ChebyshevDistance(Vector2Int a, Vector2Int b) =>
            Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));

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
        /// 4방향(상/하/좌/우)만 허용. 점유된 칸은 자동 제외. 동거리 후보가 여러 개면 랜덤 1개.
        /// </summary>
        public Vector2Int? NextStepToward(BattleUnit self, Vector2Int target)
        {
            int curDist = Distance(self.Cell, target);
            var candidates = new List<Vector2Int>(4);

            for (int i = 0; i < _dirs4.Length; i++)
            {
                var next = self.Cell + _dirs4[i];
                if (!InBounds(next) || !IsEmpty(next)) continue;
                if (Distance(next, target) < curDist) candidates.Add(next);
            }

            if (candidates.Count == 0) return null;
            return candidates[Random.Range(0, candidates.Count)];
        }
    }
}