using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public Rigidbody rigid;
    public Animator animator;

    // 설정값
    public float speed;

    // 상태값
    State nowState;
    Vector3 moveDirection;

    // 상수값
    readonly float cos45 = 0.707107f;
    readonly float sin45 = 0.707107f;

    public enum State
    {
        Idle,
        Attack
    }

    private void Start()
    {
        nowState = State.Idle;
    }

    private void Update()
    {
        if (nowState == State.Idle)
        {
            // 입력값 저장
            var input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            input.Normalize();
            moveDirection = new Vector3(input.x, 0f, input.y);

            // 애니메이션
            if (moveDirection == Vector3.zero)
                animator.SetBool("Moving", false);
            else
            {
                animator.SetBool("Moving", true);
            }
        }
    }

    private void FixedUpdate()
    {
        if (nowState == State.Idle)
        {
            var resultVelocity = rigid.linearVelocity;
            resultVelocity = moveDirection * speed;

            var moveDirectionLeft = new Vector3(moveDirection.x * cos45 - moveDirection.z * sin45, 0f, moveDirection.x * sin45 + moveDirection.z * cos45);
            var moveDirectionRight = new Vector3(moveDirection.x * cos45 + moveDirection.z * sin45, 0f, -moveDirection.x * sin45 + moveDirection.z * cos45);

            // 벽 붙어서 이동 보정
            if (Physics.Raycast(transform.position, moveDirectionLeft, out var hit, 0.52f, LayerMask.GetMask("Wall"))
                || Physics.Raycast(transform.position, moveDirectionLeft, out hit, 0.71f, LayerMask.GetMask("Wall"))
                || Physics.Raycast(transform.position, moveDirectionRight, out hit, 0.71f, LayerMask.GetMask("Wall")))
            {
                var wallNormal = hit.normal;
                var slideDirection = Vector3.ProjectOnPlane(moveDirection, wallNormal);
                resultVelocity = slideDirection.normalized * speed;
            }

            resultVelocity.y = rigid.linearVelocity.y;
            rigid.linearVelocity = resultVelocity;

            if (moveDirection != Vector3.zero)
                rigid.rotation = Quaternion.LookRotation(moveDirection);
        }
    }
}
