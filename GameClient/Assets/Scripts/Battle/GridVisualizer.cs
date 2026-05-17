using System.Collections.Generic;
using UnityEngine;

namespace AutoBattler.Battle
{
    /// <summary>
    /// 아군 배치 영역(5x4) 시각화. 셀마다 자식 GameObject에 LineRenderer를 두고
    /// 사각형 외곽선을 그림. (단일 LineRenderer는 셀 사이 대각선이 생기므로 분리.)
    ///
    /// 부착 위치: BattleField와 무관한 빈 GameObject (씬 어디든).
    /// 활성/비활성으로 토글.
    /// </summary>
    public class GridVisualizer : MonoBehaviour
    {
        public BattleField field;
        public float liftY = 0.02f;
        public Color lineColor = new Color(0.4f, 0.9f, 1f, 0.8f);
        public float lineWidth = 0.04f;
        public Material lineMaterial; // 비워두면 기본 Sprites/Default 사용

        private readonly List<LineRenderer> _cells = new List<LineRenderer>();

        private void OnEnable()
        {
            BuildIfNeeded();
            foreach (var lr in _cells) if (lr != null) lr.enabled = true;
        }

        private void OnDisable()
        {
            foreach (var lr in _cells) if (lr != null) lr.enabled = false;
        }

        private void BuildIfNeeded()
        {
            if (field == null) return;

            int cellsX = BattleGrid.Width;
            int cellsY = BattleGrid.AllyZoneMaxY + 1;
            int need = cellsX * cellsY;

            // 부족하면 생성
            while (_cells.Count < need)
            {
                var go = new GameObject($"CellLine_{_cells.Count}");
                go.transform.SetParent(transform, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.loop = true;        // ← 사각형 닫음
                lr.positionCount = 4;
                lr.startWidth = lineWidth;
                lr.endWidth = lineWidth;
                lr.startColor = lineColor;
                lr.endColor = lineColor;
                lr.material = lineMaterial != null ? lineMaterial
                                                     : new Material(Shader.Find("Sprites/Default"));
                _cells.Add(lr);
            }

            // 위치 갱신
            float halfX = field.cellSize.x * 0.5f;
            float halfZ = field.cellSize.y * 0.5f;
            int idx = 0;

            for (int y = 0; y < cellsY; y++)
                for (int x = 0; x < cellsX; x++)
                {
                    Vector3 c = field.CellToWorld(new Vector2Int(x, y));
                    c.y += liftY;
                    var lr = _cells[idx++];
                    lr.SetPosition(0, c + new Vector3(-halfX, 0, -halfZ));
                    lr.SetPosition(1, c + new Vector3(halfX, 0, -halfZ));
                    lr.SetPosition(2, c + new Vector3(halfX, 0, halfZ));
                    lr.SetPosition(3, c + new Vector3(-halfX, 0, halfZ));
                    // loop=true이므로 자동으로 4→0 연결
                }
        }
    }
}