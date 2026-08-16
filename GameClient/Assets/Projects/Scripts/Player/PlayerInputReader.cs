using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 입력 계층. 여기서는 "무엇을 눌렀는가"만 다루고 "무슨 일이 일어나는가"는 다루지 않는다.
/// 이 경계 덕분에 나중에 게임패드, 컷신 중 입력 차단, AI가 같은 캐릭터를 조종하는 것이
/// 전부 컨트롤러를 뜯지 않고 가능해진다.
///
/// 세팅: 같은 오브젝트에 PlayerInput 컴포넌트를 붙이고
///       Actions = InputSystem_Actions (Input System 패키지 기본 에셋)
///       Behavior = Send Messages
/// </summary>
public class PlayerInputReader : MonoBehaviour
{
    // ── 지속 입력 ──────────────────────────────
    public Vector2 Move { get; private set; }
    public Vector2 Look { get; private set; }
    public bool Sprint { get; private set; }

    // ── 순간 입력 (Consume로 소비) ─────────────
    bool jumpQueued;
    bool attackQueued;

    /// <summary>false면 모든 입력이 무시된다. 컷신/UI/사망 시 여기만 끄면 됨.</summary>
    public bool InputEnabled { get; set; } = true;

    /// <summary>점프 입력을 소비한다. true를 반환하면 이번 프레임에 점프가 눌린 것.</summary>
    public bool ConsumeJump()
    {
        if (!jumpQueued) return false;
        jumpQueued = false;
        return true;
    }

    /// <summary>공격 입력을 소비한다.</summary>
    public bool ConsumeAttack()
    {
        if (!attackQueued) return false;
        attackQueued = false;
        return true;
    }

    public void ClearAll()
    {
        Move = Vector2.zero;
        Look = Vector2.zero;
        Sprint = false;
        jumpQueued = false;
        attackQueued = false;
    }

    // ── PlayerInput (Send Messages) 콜백 ───────
    void OnMove(InputValue value)
    {
        Debug.Log("move");
        Move = InputEnabled ? value.Get<Vector2>() : Vector2.zero;
    }

    void OnLook(InputValue value)
    {
        Look = InputEnabled ? value.Get<Vector2>() : Vector2.zero;
    }

    void OnSprint(InputValue value)
    {
        Sprint = InputEnabled && value.isPressed;
    }

    void OnJump(InputValue value)
    {
        if (InputEnabled && value.isPressed) jumpQueued = true;
    }

    void OnAttack(InputValue value)
    {
        if (InputEnabled && value.isPressed) attackQueued = true;
    }

    void OnDisable()
    {
        ClearAll();
    }
}
