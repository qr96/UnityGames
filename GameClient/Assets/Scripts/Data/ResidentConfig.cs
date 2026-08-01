using UnityEngine;

// 주민 공통값. 시험판은 전원 동일 수치(개별 게이지·특성 없음).
[CreateAssetMenu(fileName = "ResidentConfig", menuName = "혹한/Resident Config")]
public class ResidentConfig : ScriptableObject
{
    [Tooltip("원(온기) 밖 버티기 시간(초) — 전원 동일")]
    public float outsideEnduranceSeconds = 15f;
    [Tooltip("화로 복귀 후 재출발까지 회복 시간(초)")]
    public float recoverSeconds = 5f;
    public float moveSpeed = 3.5f;
}
