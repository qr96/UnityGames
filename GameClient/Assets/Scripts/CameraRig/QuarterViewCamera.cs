using UnityEngine;

// 고정 각도 쿼터뷰 카메라. 회전 입력 없음. 대상 위치만 부드럽게 추종.
public class QuarterViewCamera : MonoBehaviour
{
    [Header("추적 대상")]
    [SerializeField] private Transform target;

    [Header("고정 시점")]
    [Tooltip("대상 기준 카메라 위치 오프셋(월드 축)")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 12f, -8f);
    [Tooltip("카메라 고정 각도(피치/요/롤)")]
    [SerializeField] private Vector3 eulerAngles = new Vector3(50f, 0f, 0f);

    [Header("따라가기")]
    [Tooltip("클수록 즉시 따라감. 0이면 즉시 스냅")]
    [SerializeField] private float followSmooth = 10f;

    private void Start()
    {
        transform.rotation = Quaternion.Euler(eulerAngles);
        if (target != null)
            transform.position = target.position + offset;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;
        if (followSmooth <= 0f)
            transform.position = desired;
        else
            transform.position = Vector3.Lerp(
                transform.position, desired, followSmooth * Time.deltaTime);

        transform.rotation = Quaternion.Euler(eulerAngles); // 고정 각도 유지
    }
}
