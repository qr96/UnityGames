using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// 카메라 추적용 타겟 + 상하 조준 보정.
///
/// 위치: 대상(Player)을 따라간다. 회전을 물려받지 않도록 씬 루트에 둔다.
/// 회전: PanTilt의 Pan(수평) 값과 동기화한다.
///
/// 하향 보정: 아래를 조준할수록 타겟을 "위로 + 앞으로" 밀어
///            카메라가 내려다보게 한다. 발밑 조준용.
/// 상향 보정: 위를 조준할수록 타겟을 "아래로" 내려
///            카메라가 올려다보게 한다. 캐릭터 머리에 하늘이 가리지 않는다.
///
/// [세팅]
/// 1. 씬 루트의 빈 오브젝트에 부착 (Player의 자식 금지)
/// 2. Follow = Player / PanTilt = 조준 카메라 / ThirdPersonFollow = 조준 카메라
/// </summary>
public class CameraFollowTarget : MonoBehaviour
{
    [Header("References")]
    [Tooltip("따라갈 대상 (Player)")]
    [SerializeField] Transform follow;

    [Tooltip("조준 카메라의 PanTilt. Pan은 회전 동기화, Tilt는 상하 보정에 쓴다")]
    [SerializeField] CinemachinePanTilt panTilt;

    [Tooltip("조준 카메라의 ThirdPersonFollow. 어깨 오프셋 보정에 쓴다")]
    [SerializeField] CinemachineThirdPersonFollow thirdPersonFollow;

    [Header("Base")]
    [Tooltip("대상 기준 높이 오프셋 (m). 상체 높이 정도")]
    [SerializeField] Vector3 offset = new Vector3(0f, 1.4f, 0f);

    [Header("하향 조준 보정 (아래를 볼 때)")]
    [Tooltip("보정이 시작되는 Tilt 각도")]
    [SerializeField] float downTiltThreshold = 10f;

    [Tooltip("최대 하향에서 타겟이 추가로 올라가는 높이 (m)")]
    [SerializeField] float maxHeightBoost = 1.6f;

    [Tooltip("하향 시 타겟을 앞으로 미는 비율. 높이 보정 1m당 앞으로 가는 거리")]
    [SerializeField] float forwardPushRatio = 0.8f;

    [Tooltip("최대 하향에서 어깨 오프셋이 추가로 벌어지는 거리 (m)")]
    [SerializeField] float maxShoulderBoost = 0.35f;

    [Header("상향 조준 보정 (위를 볼 때)")]
    [Tooltip("보정이 시작되는 상향 Tilt 각도 (절대값)")]
    [SerializeField] float upTiltThreshold = 10f;

    [Tooltip("최대 상향에서 타겟이 추가로 내려가는 높이 (m). "
           + "카메라가 허리께로 내려가 올려다보게 된다")]
    [SerializeField] float maxHeightDrop = 0.9f;

    [Header("Smoothing")]
    [Tooltip("보정이 적용되는 속도. 클수록 즉각적")]
    [SerializeField] float boostSmoothing = 10f;

    float currentHeightAdjust;    // +면 위로(하향 보정), -면 아래로(상향 보정)
    float currentShoulderBoost;
    float baseShoulderX;
    bool shoulderCached;

    void LateUpdate()
    {
        if (!follow) return;

        float dt = Time.deltaTime;

        // ── 보정량 계산 ──
        // Tilt: 아래를 볼수록 +, 위를 볼수록 -
        float targetHeightAdjust = 0f;
        float targetShoulder = 0f;

        if (panTilt)
        {
            float tilt = panTilt.TiltAxis.Value;
            float maxDown = panTilt.TiltAxis.Range.y;   // 하향 한계 (+)
            float maxUp = panTilt.TiltAxis.Range.x;     // 상향 한계 (-)

            if (tilt > downTiltThreshold && maxDown > downTiltThreshold)
            {
                // 하향: 타겟을 위로 + 어깨 벌리기
                float t = Mathf.InverseLerp(downTiltThreshold, maxDown, tilt);
                targetHeightAdjust = maxHeightBoost * t;
                targetShoulder = maxShoulderBoost * t;
            }
            else if (tilt < -upTiltThreshold && maxUp < -upTiltThreshold)
            {
                // 상향: 타겟을 아래로
                float t = Mathf.InverseLerp(-upTiltThreshold, maxUp, tilt);
                targetHeightAdjust = -maxHeightDrop * t;
            }
        }

        // 부드럽게 적용
        float lerp = 1f - Mathf.Exp(-boostSmoothing * dt);
        currentHeightAdjust = Mathf.Lerp(currentHeightAdjust, targetHeightAdjust, lerp);
        currentShoulderBoost = Mathf.Lerp(currentShoulderBoost, targetShoulder, lerp);

        // ── 위치 ──
        // 하향일 때만 앞으로 민다 (상향에서는 밀 필요 없음)
        Vector3 forwardPush = Vector3.zero;
        if (panTilt && currentHeightAdjust > 0.01f)
        {
            Vector3 aimForward = Quaternion.Euler(0f, panTilt.PanAxis.Value, 0f) * Vector3.forward;
            forwardPush = aimForward * (currentHeightAdjust * forwardPushRatio);
        }

        transform.position = follow.position + offset
                           + Vector3.up * currentHeightAdjust
                           + forwardPush;

        // ── 회전: 수평만 PanTilt와 동기화 ──
        if (panTilt)
            transform.rotation = Quaternion.Euler(0f, panTilt.PanAxis.Value, 0f);

        // ── 어깨 오프셋 보정 ──
        if (thirdPersonFollow)
        {
            if (!shoulderCached)
            {
                baseShoulderX = thirdPersonFollow.ShoulderOffset.x;
                shoulderCached = true;
            }

            var shoulder = thirdPersonFollow.ShoulderOffset;
            shoulder.x = baseShoulderX + currentShoulderBoost;
            thirdPersonFollow.ShoulderOffset = shoulder;
        }
    }

    void OnValidate()
    {
        if (transform.parent)
            Debug.LogWarning(
                $"{name}: 이 오브젝트는 씬 루트에 있어야 한다. " +
                "부모(특히 Player)의 자식이면 회전을 물려받아 조준이 잠긴다.", this);
    }
}