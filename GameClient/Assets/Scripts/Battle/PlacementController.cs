using UnityEngine;
using AutoBattler.Rounds;
using AutoBattler.Heroes;
using AutoBattler.Core;

namespace AutoBattler.Battle
{
    /// <summary>
    /// 배치 화면 컨트롤러. 활성 상태일 때만 입력을 받음.
    ///
    /// 흐름:
    ///   1) 마우스 다운/터치 시작 → Raycast 로 영웅 BattleUnit 찾기
    ///   2) 드래그 중 → 그라운드(Y=groundY) 평면과 교차점 계산 → 영웅 위치 따라감
    ///   3) 마우스 업 → 가장 가까운 셀(아군 영역만)에 스냅
    ///      - 그 셀에 다른 영웅이 있으면 RunManager.SetPlacement 가 swap 처리
    ///      - 아군 영역 밖이면 원래 자리로 복귀
    ///   4) RunManager.SetPlacement 호출 + BattleField 미리보기 재구성
    ///
    /// 사용: 빈 GameObject 에 부착, runManager / battleField / camera 슬롯 연결.
    /// </summary>
    public class PlacementController : MonoBehaviour
    {
        [Header("연결")]
        public RunManager   runManager;
        public BattleField  battleField;
        public Camera       cam;
        public LayerMask    unitLayerMask = ~0; // 영웅에 부착할 레이어 (기본: 전체)

        [Header("드래그 시각")]
        public float dragLiftY = 0.6f;   // 드래그 중 살짝 띄움 (월드 Y)

        // 상태
        private BattleUnit _dragging;
        private Vector2Int _originalCell;
        private Vector3    _originalWorld;

        private void Awake()
        {
            if (cam == null) cam = Camera.main;
        }

        private void OnEnable()
        {
            // 배치 화면 진입 시 미리보기 시작 (RunManager 영웅들을 그리드에 올림)
            if (battleField != null && runManager != null)
                battleField.EnterPlacementPreview(runManager.Roster, runManager.Placement);
        }

        private void OnDisable()
        {
            // 배치 종료 시 미리보기 정리
            if (battleField != null) battleField.ExitPlacementPreview();
            _dragging = null;
        }

        // ─────────────────────────────────────────────────────────
        private void Update()
        {
            if (cam == null || battleField == null || runManager == null) return;

            // 시작
            if (Input.GetMouseButtonDown(0))
                TryBeginDrag();

            // 진행
            if (_dragging != null && Input.GetMouseButton(0))
                UpdateDrag();

            // 종료
            if (Input.GetMouseButtonUp(0) && _dragging != null)
                EndDrag();
        }

        private void TryBeginDrag()
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 100f, unitLayerMask))
            {
                var unit = hit.collider.GetComponentInParent<BattleUnit>();
                if (unit != null && unit.Team == Team.Ally)
                {
                    _dragging      = unit;
                    _originalCell  = unit.Cell;
                    _originalWorld = unit.transform.position;
                }
            }
        }

        private void UpdateDrag()
        {
            // 그라운드 평면과 교차
            var plane = new Plane(Vector3.up, new Vector3(0, battleField.groundY, 0));
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (plane.Raycast(ray, out float t))
            {
                Vector3 hit = ray.GetPoint(t);
                hit.y += dragLiftY; // 들어올림 효과
                _dragging.transform.position = hit;
            }
        }

        private void EndDrag()
        {
            // 마우스 위치에서 셀 계산
            var plane = new Plane(Vector3.up, new Vector3(0, battleField.groundY, 0));
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (!plane.Raycast(ray, out float t))
            {
                Cancel();
                return;
            }
            Vector3 worldHit = ray.GetPoint(t);
            Vector2Int targetCell = battleField.WorldToCell(worldHit);

            // 아군 영역 밖 → 취소
            if (targetCell.y > BattleGrid.AllyZoneMaxY)
            {
                Cancel();
                return;
            }

            // 제자리 → 그냥 스냅 복귀
            if (targetCell == _originalCell)
            {
                _dragging.transform.position = battleField.CellToWorld(_originalCell);
                _dragging = null;
                return;
            }

            // 배치 변경 (swap 자동 처리)
            runManager.SetPlacement(_dragging.SourceHero, targetCell);

            // 그리드 + 시각 위치 재구성을 가장 확실하게: 미리보기 다시 만들기
            battleField.EnterPlacementPreview(runManager.Roster, runManager.Placement);
            _dragging = null;
        }

        private void Cancel()
        {
            if (_dragging != null)
            {
                _dragging.transform.position = battleField.CellToWorld(_originalCell);
                _dragging = null;
            }
        }
    }
}
