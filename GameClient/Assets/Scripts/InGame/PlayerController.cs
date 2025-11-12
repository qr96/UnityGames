using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace InGame
{
    public class PlayerController : MonoBehaviour
    {
        public Rigidbody rigid;
        public Animator animator;

        public TriggerEvent attackTrigger;

        // 설정값
        public float speed;
        public float attackCoolTime;
        public float repulsivePower;

        // 상태값
        State nowState;
        Vector3 moveDirection;
        DateTime attackEnd;
        HashSet<EnemyController> enemies = new HashSet<EnemyController>();

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
            attackTrigger.Set(OnEnterAttackTrigger, OnExitAttackTrigger);
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

                if (enemies.Count > 0)
                {
                    var inputAttackVector = moveDirection;
                    var isAttack = false;
                    var lastEnemyVector = Vector3.zero;

                    // 타겟 공격
                    foreach (var enemy in enemies)
                    {
                        var enemyVector = (enemy.transform.position - transform.position);
                        enemyVector.y = 0f;
                        enemyVector.Normalize();
                        isAttack = Vector3.Dot(enemyVector, inputAttackVector) > 0f;
                        enemy.OnAttacked(isAttack ? inputAttackVector : enemyVector, gameObject);
                        lastEnemyVector = enemyVector;
                    }

                    // 비활성화된 타겟 목록에서 제거 (반드시 공격 직후에 해줘야함)
                    enemies.RemoveWhere(enemy => !enemy.isActiveAndEnabled);

                    // 공격 애니메이션
                    animator.SetTrigger("Attack");
                    animator.SetBool("Moving", false);
                    attackEnd = DateTime.Now.AddSeconds(attackCoolTime);
                    OnPushed(isAttack ? -inputAttackVector : -lastEnemyVector);

                    // 상태 변경
                    nowState = State.Attack;
                }
                else
                {
                    // 애니메이션
                    animator.SetBool("Moving", moveDirection != Vector3.zero);
                }
            }
            else if (nowState == State.Attack)
            {
                if (DateTime.Now > attackEnd)
                    nowState = State.Idle;
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

        void OnEnterAttackTrigger(Collider col)
        {
            if (col.CompareTag("Enemy"))
            {
                var enemy = col.GetComponent<EnemyController>();
                enemies.Add(enemy);
            }
        }

        void OnExitAttackTrigger(Collider col)
        {
            if (col.CompareTag("Enemy"))
            {
                var enemy = col.GetComponent<EnemyController>();
                if (enemies.Contains(enemy))
                    enemies.Remove(enemy);
            }
        }

        void OnPushed(Vector3 pushed)
        {
            rigid.linearVelocity = new Vector3(0f, rigid.linearVelocity.y, 0f);
            rigid.AddForce(pushed * repulsivePower, ForceMode.Impulse);
        }
    }
}
