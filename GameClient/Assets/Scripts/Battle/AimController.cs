using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 조준 입력 + 조준선 표시.
/// - 신/구 Input System 양쪽 대응 (컴파일 디파인으로 자동 분기)
/// - 터치 지점 → 카메라 레이 → 게임플레이 평면 교차점으로 조준 (카메라 각도 무관)
/// - ReflectionSolver.Simulate 공유로 미리보기 == 실제 궤적 보장
/// </summary>
public class AimController : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] BattleManager battle;
    [SerializeField] LineRenderer aimLine;

    [Header("조준 설정")]
    [Tooltip("+z(정면) 기준 최소 발사각(도)")]
    [SerializeField] float minAngleDeg = 8f;
    [SerializeField] int previewBounces = 1;
    [SerializeField] float previewMaxDistance = 60f;
    // 충돌 마스크는 BattleManager.CollisionMask 단일 소스 사용 (중복 설정 실수 방지)

    [Header("디버그")]
    [Tooltip("조준이 막히는 원인을 프레임당 1회 로그")]
    [SerializeField] bool debugLog = false;

    Camera _cam;
    bool _aiming;
    Vector3 _aimDir = Vector3.forward;
    readonly List<Vector3> _points = new List<Vector3>(8);
    readonly List<ReflectionSolver.Hit> _hits = new List<ReflectionSolver.Hit>(4);

    public bool HasValidAim { get; private set; }

    // ---------------- 입력 추상화 (신/구 백엔드) ----------------

    static bool PointerPressed()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
        return false;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    static bool PointerHeld()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (Mouse.current != null && Mouse.current.leftButton.isPressed) return true;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed) return true;
        return false;
#else
        return Input.GetMouseButton(0);
#endif
    }

    static bool PointerReleased()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame) return true;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame) return true;
        return false;
#else
        return Input.GetMouseButtonUp(0);
#endif
    }

    static Vector3 PointerPosition()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            return Touchscreen.current.primaryTouch.position.ReadValue();
        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();
        return Vector3.zero;
#else
        return Input.mousePosition;
#endif
    }

    /// <summary>포인터가 UI 위에 있으면 true — HUD 버튼 탭이 조준으로 새는 것 방지.</summary>
    static bool PointerOverUI()
    {
        var es = EventSystem.current;
        if (es == null) return false;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        return es.IsPointerOverGameObject();
#else
        // 구 입력: 터치는 fingerId 기준으로 확인해야 정확
        if (Input.touchCount > 0)
            return es.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        return es.IsPointerOverGameObject();
#endif
    }

    // ---------------- 라이프사이클 ----------------

    void Awake()
    {
        _cam = Camera.main;

        // 자가 진단: 셋업 실수는 시작 시점에 명시적으로 터뜨린다
        if (_cam == null)
            Debug.LogError("[AimController] Camera.main이 null. Main Camera 태그 확인.", this);
        if (battle == null)
            Debug.LogError("[AimController] battle 레퍼런스 미할당.", this);
        if (aimLine == null)
            Debug.LogError("[AimController] aimLine 레퍼런스 미할당.", this);

        if (aimLine != null)
        {
            aimLine.enabled = false;
            aimLine.useWorldSpace = true;
        }
    }

    void Update()
    {
        if (battle == null || _cam == null || aimLine == null) return;

        if (!battle.CanAim)
        {
            if (debugLog && PointerHeld())
                Debug.Log($"[Aim] 차단: BattleManager.Current = {battle.Current}");
            CancelAim();
            return;
        }

        if (PointerPressed() && !PointerOverUI()) _aiming = true;

        if (_aiming && PointerHeld())
            UpdateAim(PointerPosition());

        if (_aiming && PointerReleased())
        {
            _aiming = false;
            aimLine.enabled = false;
            if (HasValidAim)
                battle.RequestFire(_aimDir);
            else if (debugLog)
                Debug.Log("[Aim] 발사 취소: 유효하지 않은 각도에서 릴리즈");
        }
    }

    // ---------------- 조준 계산 ----------------

    void UpdateAim(Vector3 screenPos)
    {
        Vector3 launchPos = battle.LaunchPosition;

        // 화면 좌표 → 게임플레이 평면(y = launchPos.y) 교차점
        Plane gameplayPlane = new Plane(Vector3.up, new Vector3(0f, launchPos.y, 0f));
        Ray ray = _cam.ScreenPointToRay(screenPos);
        if (!gameplayPlane.Raycast(ray, out float enter))
        {
            SetInvalid("카메라 레이가 게임플레이 평면과 교차하지 않음 (카메라 각도 확인)");
            return;
        }

        Vector3 dir = ray.GetPoint(enter) - launchPos;
        dir.y = 0f;

        // 전방(+z) 기준 최소각 검증
        float angleFromForward = Vector3.Angle(Vector3.forward, dir);
        bool valid = dir.sqrMagnitude > 1e-4f && angleFromForward < 90f - minAngleDeg;
        if (!valid)
        {
            SetInvalid($"각도 게이트: forward와 {angleFromForward:F1}° (허용 < {90f - minAngleDeg}°)");
            return;
        }

        HasValidAim = true;
        _aimDir = dir.normalized;

        ReflectionSolver.Simulate(
            launchPos, _aimDir, battle.CurrentHero.projectileRadius,
            battle.CollisionMask, previewBounces, previewMaxDistance,
            float.NegativeInfinity,
            _points, _hits);

        aimLine.enabled = true;
        aimLine.positionCount = _points.Count;
        for (int i = 0; i < _points.Count; i++)
            aimLine.SetPosition(i, _points[i]);

        // debugLog 시 Scene 뷰에 궤적 직접 표시.
        // 여기(초록선)는 보이는데 Game 뷰 조준선이 안 보이면 → LineRenderer 설정 문제로 확정.
        if (debugLog)
            for (int i = 0; i < _points.Count - 1; i++)
                Debug.DrawLine(_points[i], _points[i + 1], Color.green);
    }

    void SetInvalid(string reason)
    {
        HasValidAim = false;
        aimLine.enabled = false;
        if (debugLog) Debug.Log($"[Aim] 무효: {reason}");
    }

    void CancelAim()
    {
        _aiming = false;
        HasValidAim = false;
        aimLine.enabled = false;
    }
}