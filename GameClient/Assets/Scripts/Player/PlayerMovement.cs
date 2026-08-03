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

    [Header("지면 맞춤 (격자 모드)")]
    [Tooltip("캡슐 밑면을 지면에 맞춘 뒤 추가로 올릴 값. 모델이 가라앉으면 늘리기")]
    [SerializeField] private float groundExtraOffset = 0f;

    [Header("무게")]
    [Tooltip("비우면 씬에서 찾음. 없으면 감속 없음")]
    [SerializeField] private Inventory inventory;

    private CharacterController controller;
    private float verticalVelocity;
    private WorldGrid grid;

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
        grid = WorldGrid.Instance != null ? WorldGrid.Instance : FindObjectOfType<WorldGrid>();
    }

    // CharacterController 캡슐 밑면 → 오브젝트 원점까지의 거리
    private float FootToOriginOffset()
        => controller != null ? (controller.height * 0.5f - controller.center.y) : 0f;

    // 격자 규칙상 갈 수 없는 방향 성분을 제거
    private Vector3 FilterByGrid(Vector3 delta)
    {
        Vector3 from = transform.position;

        if (grid.CanMoveToWorld(from, from + delta)) return delta;

        // 대각선이 막히면 축별로 시도
        Vector3 xOnly = new Vector3(delta.x, 0f, 0f);
        Vector3 zOnly = new Vector3(0f, 0f, delta.z);

        bool okX = Mathf.Abs(delta.x) > 0.0001f && grid.CanMoveToWorld(from, from + xOnly);
        bool okZ = Mathf.Abs(delta.z) > 0.0001f && grid.CanMoveToWorld(from, from + zOnly);

        if (okX && okZ) return delta;   // 둘 다 되면 그대로(모서리 통과 허용)
        if (okX) return xOnly;
        if (okZ) return zOnly;
        return Vector3.zero;
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

        Vector3 horizontal = inputDir * speed * Time.deltaTime;

        if (grid != null)
        {
            // 격자 통행 판정: 막히면 축을 나눠 벽을 따라 미끄러짐 (이동 자체는 연속)
            horizontal = FilterByGrid(horizontal);

            // 층 높이를 따라감(중력 대신) — 절벽 위/아래 높이 반영
            Vector3 next = transform.position + horizontal;
            float groundY = grid.SampleHeight(next); // 경사로에서는 칸 안에서 보간된 높이

            // 캡슐 밑면이 지면에 닿도록 오브젝트 원점을 올린다
            float targetY = groundY + FootToOriginOffset() + groundExtraOffset;
            float dy = Mathf.Lerp(transform.position.y, targetY, 12f * Time.deltaTime) - transform.position.y;
            controller.Move(horizontal + Vector3.up * dy);
        }
        else
        {
            Vector3 velocity = inputDir * speed;
            velocity.y = verticalVelocity;
            controller.Move(velocity * Time.deltaTime);
        }

        if (inputDir.sqrMagnitude > 0.0001f)
        {
            Quaternion target = Quaternion.LookRotation(inputDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, target, rotationSpeed * Time.deltaTime);
        }
    }
}