using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 입력 계층.
///
/// 이동/시점/점프는 PlayerInput(Send Messages)으로 받고,
/// 마우스 버튼은 Mouse.current를 직접 읽는다.
///
/// 버튼을 직접 읽는 이유: Input Actions 에셋의 인터랙션 설정(Press Only 등)에 따라
/// 뗄 때 콜백이 오지 않는 경우가 있어 홀드/릴리즈 판정이 깨진다.
/// 에셋 설정과 무관하게 동작하도록 폴링으로 처리한다.
/// (Aim 액션을 따로 만들 필요도 없다)
///
/// 조작:
///   좌클릭 홀드 → 조준 + 시위 당김
///   좌클릭 뗌   → 발사
///   우클릭      → 조준 취소
/// </summary>
public class PlayerInputReader : MonoBehaviour
{
    // ── 지속 입력 ──────────────────────────────
    public Vector2 Move { get; private set; }
    public Vector2 Look { get; private set; }
    public bool Sprint { get; private set; }

    /// <summary>좌클릭을 누르고 있는가</summary>
    public bool AttackHeld { get; private set; }

    // ── 순간 입력 ──────────────────────────────
    bool jumpQueued;
    bool attackReleasedQueued;
    bool cancelQueued;

    public bool InputEnabled { get; set; } = true;

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || !InputEnabled)
        {
            AttackHeld = false;
            return;
        }

        AttackHeld = mouse.leftButton.isPressed;

        if (mouse.leftButton.wasReleasedThisFrame) attackReleasedQueued = true;
        if (mouse.rightButton.wasPressedThisFrame) cancelQueued = true;
    }

    public bool ConsumeJump()
    {
        if (!jumpQueued) return false;
        jumpQueued = false;
        return true;
    }

    /// <summary>좌클릭을 뗀 순간. 발사 트리거.</summary>
    public bool ConsumeAttackReleased()
    {
        if (!attackReleasedQueued) return false;
        attackReleasedQueued = false;
        return true;
    }

    /// <summary>우클릭. 조준 취소.</summary>
    public bool ConsumeCancel()
    {
        if (!cancelQueued) return false;
        cancelQueued = false;
        return true;
    }

    public void ClearAll()
    {
        Move = Vector2.zero;
        Look = Vector2.zero;
        Sprint = false;
        AttackHeld = false;
        jumpQueued = false;
        attackReleasedQueued = false;
        cancelQueued = false;
    }

    // ── PlayerInput (Send Messages) 콜백 ───────
    void OnMove(InputValue value) => Move = InputEnabled ? value.Get<Vector2>() : Vector2.zero;
    void OnLook(InputValue value) => Look = InputEnabled ? value.Get<Vector2>() : Vector2.zero;
    void OnSprint(InputValue value) => Sprint = InputEnabled && value.isPressed;

    void OnJump(InputValue value)
    {
        if (InputEnabled && value.isPressed) jumpQueued = true;
    }

    void OnDisable() => ClearAll();
}