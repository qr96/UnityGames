using UnityEngine;

/// <summary>
/// 공격 단계에 맞춰 무기 트랜스폼을 회전시킨다.
/// 표현 계층이므로 판정에는 전혀 관여하지 않는다.
/// (판정은 MeleeWeapon이 캐릭터 정면 기준으로 따로 계산한다)
///
/// 사용: Player 밑에 빈 오브젝트 'WeaponPivot'을 만들고 이 컴포넌트를 붙인다.
///       그 아래에 몽둥이 큐브를 자식으로 둔다.
///
///   Player
///    └ WeaponPivot        ← 이 컴포넌트. Position (0.3, 1.1, 0)
///       └ Club (Cube)     ← Scale (0.08, 0.08, 1.1), Position (0, 0, 0.55)
/// </summary>
public class WeaponSwing : MonoBehaviour
{
    [Header("Pose (로컬 오일러 각)")]
    [Tooltip("평상시 자세")]
    [SerializeField] Vector3 idlePose = new Vector3(20f, 0f, 0f);

    [Tooltip("선딜에서 젖히는 자세")]
    [SerializeField] Vector3 windupPose = new Vector3(-110f, -30f, 0f);

    [Tooltip("판정이 끝나는 지점의 자세")]
    [SerializeField] Vector3 strikePose = new Vector3(75f, 25f, 0f);

    [Header("Curve")]
    [Tooltip("판정 구간의 스윙 가속. 초반이 빠르면 날카로워진다")]
    [SerializeField] AnimationCurve strikeCurve =
        new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 4f),
            new Keyframe(1f, 1f, 0.2f, 0.2f));

    Quaternion idleRot, windupRot, strikeRot;

    void Awake()
    {
        idleRot = Quaternion.Euler(idlePose);
        windupRot = Quaternion.Euler(windupPose);
        strikeRot = Quaternion.Euler(strikePose);
        transform.localRotation = idleRot;
    }

    /// <summary>선딜. t는 0에서 1로 진행</summary>
    public void PoseWindup(float t)
    {
        transform.localRotation = Quaternion.Slerp(idleRot, windupRot, Smooth(t));
    }

    /// <summary>판정. 젖힌 자세에서 타격 자세로 빠르게 지나간다</summary>
    public void PoseStrike(float t)
    {
        transform.localRotation = Quaternion.Slerp(windupRot, strikeRot, strikeCurve.Evaluate(t));
    }

    /// <summary>후딜. 평상시 자세로 복귀</summary>
    public void PoseRecovery(float t)
    {
        transform.localRotation = Quaternion.Slerp(strikeRot, idleRot, Smooth(t));
    }

    /// <summary>공격이 아닐 때 서서히 평상시 자세로</summary>
    public void PoseIdle(float dt)
    {
        transform.localRotation = Quaternion.Slerp(
            transform.localRotation, idleRot, 1f - Mathf.Exp(-12f * dt));
    }

    static float Smooth(float t) => t * t * (3f - 2f * t);
}
