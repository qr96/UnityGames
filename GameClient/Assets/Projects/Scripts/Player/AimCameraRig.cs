using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// 조준 카메라 전환.
///
/// SetActive로 카메라를 껐다 켜면 Cinemachine이 매번 새로 활성화된 카메라로
/// 취급해서, 블렌드가 끝날 때까지 마우스 입력이 반영되지 않는다.
/// 두 카메라를 항상 켜둔 채 Priority만 바꾸면 전환 중에도 조준이 살아 있다.
///
/// 사용: 빈 오브젝트에 붙이고 두 카메라를 꽂는다.
///       두 카메라 모두 씬에서 활성 상태로 둘 것.
/// </summary>
public class AimCameraRig : MonoBehaviour
{
    [SerializeField] CinemachineCamera followCamera;   // 평상시 3인칭
    [SerializeField] CinemachineCamera aimCamera;      // 조준용 어깨너머

    [Tooltip("활성 카메라의 우선순위")]
    [SerializeField] int activePriority = 20;

    [Tooltip("비활성 카메라의 우선순위")]
    [SerializeField] int inactivePriority = 10;

    void Awake()
    {
        if (!followCamera || !aimCamera)
            Debug.LogError($"{name}: 카메라 두 개를 모두 꽂아야 한다.", this);

        SetAiming(false);
    }

    /// <summary>조준 상태 전환. PlayerController가 호출한다.</summary>
    public void SetAiming(bool aiming)
    {
        if (followCamera) followCamera.Priority = aiming ? inactivePriority : activePriority;
        if (aimCamera)    aimCamera.Priority    = aiming ? activePriority : inactivePriority;
    }
}
