using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("이동")]
    public float moveSpeed = 5f;

    Rigidbody rigid;
    Animator animator;
    Vector3 moveDirection;
    Vector3 last;
    PlayerCombat combat;

    void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        combat = GetComponent<PlayerCombat>();

        Application.targetFrameRate = 30;
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // 카메라 기준 이동 방향 계산
        moveDirection = new Vector3(h, 0f, v).normalized;
        
        // 애니메이션
        if (animator != null)
            animator.SetBool("isWalking", moveDirection.magnitude > 0.1f);

        // 캐릭터 방향 전환
        if (moveDirection != Vector3.zero && !combat.IsAttacking)
        {
            rigid.rotation = Quaternion.LookRotation(moveDirection);
            last = moveDirection;
        }
    }

    private void FixedUpdate()
    {
        var resultVelocity = moveDirection * moveSpeed;
        resultVelocity.y = rigid.linearVelocity.y;
        rigid.linearVelocity = resultVelocity;
    }
}
