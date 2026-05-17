using UnityEngine;

namespace AutoBattler.Battle
{
    /// <summary>
    /// 아군 배치 영역(5x4)을 시각화. 배치 화면에서만 보이게 토글.
    /// 각 셀을 외곽선으로 표시. groundY 약간 위에 그려서 z-fight 회피.
    ///
    /// 사용: 빈 GameObject에 부착 → field 슬롯에 BattleField 드래그.
    ///       기본적으론 비활성(SetActive false)으로 두고, 배치 화면 진입 시 활성.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class GridVisualizer : MonoBehaviour
    {
        public BattleField field;
        public float liftY = 0.02f;          // 바닥 살짝 위
        public Color   lineColor = new Color(0.4f, 0.9f, 1f, 0.8f);
        public float   lineWidth = 0.04f;

        private LineRenderer _lr;

        private void Reset() { _lr = GetComponent<LineRenderer>(); }
        private void Awake() { _lr = GetComponent<LineRenderer>(); }

        private void OnEnable()
        {
            BuildLines();
        }

        public void BuildLines()
        {
            if (field == null) return;
            if (_lr == null) _lr = GetComponent<LineRenderer>();

            _lr.useWorldSpace = true;
            _lr.startWidth = lineWidth;
            _lr.endWidth   = lineWidth;
            _lr.startColor = lineColor;
            _lr.endColor   = lineColor;
            _lr.loop = false;

            // 각 셀을 사각형으로 한 번에 그리려면 LineStrip 트릭이 필요.
            // 단순히 각 셀 4변을 따로 그리는 게 보기 쉬움 → 셀 N개 × 5점(닫힌 사각형).
            int cellsX = BattleGrid.Width;
            int cellsY = BattleGrid.AllyZoneMaxY + 1;
            int pointsPerCell = 5;
            int total = cellsX * cellsY * pointsPerCell;
            _lr.positionCount = total;

            float halfX = field.cellSize.x * 0.5f;
            float halfZ = field.cellSize.y * 0.5f;
            int idx = 0;

            for (int y = 0; y < cellsY; y++)
            for (int x = 0; x < cellsX; x++)
            {
                Vector3 c = field.CellToWorld(new Vector2Int(x, y));
                c.y += liftY;

                // 시계 방향 닫힌 사각형
                _lr.SetPosition(idx++, c + new Vector3(-halfX, 0, -halfZ));
                _lr.SetPosition(idx++, c + new Vector3( halfX, 0, -halfZ));
                _lr.SetPosition(idx++, c + new Vector3( halfX, 0,  halfZ));
                _lr.SetPosition(idx++, c + new Vector3(-halfX, 0,  halfZ));
                _lr.SetPosition(idx++, c + new Vector3(-halfX, 0, -halfZ));
            }
        }
    }
}
