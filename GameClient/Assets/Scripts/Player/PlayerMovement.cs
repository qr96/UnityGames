using UnityEngine;

// 쿼터뷰 이동. 물리(Rigidbody) 미사용. CharacterController로 충돌/중력 처리.
// 입력은 카메라 시점 기준으로 변환. 인벤토리 무게 한계 초과 시 이동 속도 감소.
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("이동")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private float gravity = -20f;

    [Header("카메라 기준")]
    [SerializeField] private Transform cameraTransform;

    [Header("달리기")]
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [Tooltip("달릴 때 속도 배율")]
    [SerializeField] private float sprintMultiplier = 1.6f;
    [SerializeField] private PlayerStats stats; // 비우면 자기/씬에서 찾음

    [Header("무게")]
    [Tooltip("비우면 씬에서 찾음. 없으면 감속 없음")]
    [SerializeField] private Inventory inventory;

    private CharacterController controller;
    private float verticalVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Start()
    {
        if (inventory == null) inventory = FindObjectOfType<Inventory>();
        if (stats == null) stats = GetComponent<PlayerStats>();
        if (stats == null) stats = FindObjectOfType<PlayerStats>();
    }

    private void Update()
    {
        if (cameraTransform == null)
        {
            if (Camera.main == null) return;
            cameraTransform = Camera.main.transform;
        }

        // 격자 등 모달 UI가 열려 있으면 이동 입력 무시(중력은 계속 적용)
        bool locked = UIInputLock.IsBlocked;

        float h = locked ? 0f : Input.GetAxisRaw("Horizontal");
        float v = locked ? 0f : Input.GetAxisRaw("Vertical");

        Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;

        Vector3 inputDir = camForward * v + camRight * h;
        if (inputDir.sqrMagnitude > 1f) inputDir.Normalize();

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        // 달리기: 이동 입력이 있고 기력이 남아 있을 때만
        bool wantSprint = !locked
                          && Input.GetKey(sprintKey)
                          && inputDir.sqrMagnitude > 0.0001f
                          && stats != null && stats.CanSprint;
        if (stats != null) stats.SetSprinting(wantSprint);

        float speed = moveSpeed * (inventory != null ? inventory.SpeedMultiplier : 1f);
        if (wantSprint) speed *= Mathf.Max(1f, sprintMultiplier);

        Vector3 velocity = inputDir * speed;
        velocity.y = verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

        if (inputDir.sqrMagnitude > 0.0001f)
        {
            Quaternion target = Quaternion.LookRotation(inputDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, target, rotationSpeed * Time.deltaTime);
        }
    }
}