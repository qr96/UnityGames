using UnityEngine;

namespace AutoBattler.Battle
{
    /// <summary>
    /// 탑다운 전투 카메라.
    /// BattleField의 그리드 중앙 위에서 살짝 기울여 비춤.
    ///
    /// 권장 셋업:
    ///   - Camera Projection: Perspective (FOV ~ 35~50)
    ///     기울임 적고 거리감 줄이고 싶으면 Orthographic + size 6~8 추천
    ///   - tiltDegrees 60~90 (90 = 완전 수직 탑다운, 60 = 살짝 비스듬한 쿼터뷰 느낌)
    ///
    /// BattleField 의 cellSize/그리드 크기에 맞춰 자동으로 거리 산정.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class TopDownBattleCamera : MonoBehaviour
    {
        public BattleField field;

        [Header("앵글")]
        [Range(45f, 90f)] public float tiltDegrees = 75f; // 90 = 완전 위에서
        public float yaw = 0f;                            // 보드 회전 (보통 0)

        [Header("거리")]
        [Tooltip("그리드 짧은 변의 몇 배만큼 떨어질지")]
        public float distanceMultiplier = 1.4f;

        [Header("따라가기")]
        public bool followCenter = true;     // 그리드 중앙 자동 추적
        public Vector3 manualCenter;         // followCenter = false 일 때 수동 위치
        public float followLerp = 8f;

        private Camera _cam;

        private void Awake() { _cam = GetComponent<Camera>(); }

        private void LateUpdate()
        {
            Vector3 center = followCenter && field != null
                ? GetGridCenter()
                : manualCenter;

            // 그리드 짧은 변 기준 거리
            float gridShort = Mathf.Min(
                BattleGrid.Width  * (field != null ? field.cellSize.x : 1.1f),
                BattleGrid.Height * (field != null ? field.cellSize.y : 1.1f));
            float dist = gridShort * distanceMultiplier;

            // 카메라 위치/회전 계산
            Quaternion rot = Quaternion.Euler(tiltDegrees, yaw, 0f);
            Vector3 desiredPos = center - rot * Vector3.forward * dist;

            float t = 1f - Mathf.Exp(-followLerp * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPos, t);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, t);
        }

        private Vector3 GetGridCenter()
        {
            return field.CellToWorld(new Vector2Int(
                BattleGrid.Width / 2, BattleGrid.Height / 2));
        }
    }
}
