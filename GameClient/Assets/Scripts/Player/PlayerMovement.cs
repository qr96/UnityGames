using UnityEngine;

// 쿼터뷰 이동. 물리(Rigidbody) 미사용. CharacterController로 충돌/중력 처리.
// 입력은 카메라 시점 기준으로 변환하여 화면 방향과 일치시킴.
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("이동")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 720f; // 초당 회전 각도(도)
    [SerializeField] private float gravity = -20f;

    [Header("카메라 기준")]
    [Tooltip("비워두면 시작 시 Camera.main 사용")]
    [SerializeField] private Transform cameraTransform;

    private CharacterController controller;
    private float verticalVelocity; // 중력 누적(y)

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        if (cameraTransform == null)
        {
            if (Camera.main == null) return;
            cameraTransform = Camera.main.transform;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // 카메라 forward/right를 XZ 평면에 눕혀 이동 기준축으로 사용
        Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 camRight   = Vector3.ProjectOnPlane(cameraTransform.right,   Vector3.up).normalized;

        Vector3 inputDir = camForward * v + camRight * h;
        if (inputDir.sqrMagnitude > 1f) inputDir.Normalize();

        // 중력 (CharacterController)
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f; // 바닥에 붙이는 소량 하강력
        verticalVelocity += gravity * Time.deltaTime;

        // 수평 속도 + 수직 속도 → 한 번에 이동
        Vector3 velocity = inputDir * moveSpeed;
        velocity.y = verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

        // 이동 방향 바라보기
        if (inputDir.sqrMagnitude > 0.0001f)
        {
            Quaternion target = Quaternion.LookRotation(inputDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, target, rotationSpeed * Time.deltaTime);
        }
    }
}
