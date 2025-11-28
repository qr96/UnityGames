using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        public float fullHealDuration;

        // 상태값
        State nowState;
        Vector3 moveDirection;
        float attackEnd;

        HashSet<EnemyController> enemies = new HashSet<EnemyController>();

        // 상수값
        readonly float cos45 = 0.707107f;
        readonly float sin45 = 0.707107f;

        public enum State
        {
            None,
            Idle,
            Attack
        }

        private void Start()
        {
            attackTrigger.Set(OnEnterAttackTrigger, OnExitAttackTrigger);
            SetState(State.Idle);
        }

        private void OnEnable()
        {
            PlayerDataManager.Instance.OnLevelChanged += LevelUpEffect;
        }

        private void OnDisable()
        {
            PlayerDataManager.Instance.OnLevelChanged -= LevelUpEffect;
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
                        if (enemy == null)
                        {
                            Debug.LogError("[PlayerController] enemy is null");
                            return;
                        }

                        var enemyVector = (enemy.transform.position - transform.position);
                        enemyVector.y = 0f;
                        enemyVector.Normalize();
                        isAttack = Vector3.Dot(enemyVector, inputAttackVector) > 0f;
                        enemy.OnAttacked(isAttack ? inputAttackVector : enemyVector, gameObject);
                        PlayerDataManager.Instance.ReduceMp(1);
                        lastEnemyVector = enemyVector;

                        if (PlayerDataManager.Instance.Model.NowMp <= 0)
                        {
                            SetState(State.None);
                            PlayerDataManager.Instance.Respawn();
                            SceneLoader.Instance.LoadScene("InGameScene");
                            return;
                        }
                    }

                    // 비활성화된 타겟 목록에서 제거 (반드시 공격 직후에 해줘야함)
                    enemies.RemoveWhere(enemy => !enemy.isActiveAndEnabled);

                    // 공격 애니메이션
                    animator.SetTrigger("Attack");
                    animator.SetBool("Moving", false);
                    attackEnd = Time.time + attackCoolTime;
                    OnPushed(isAttack ? -inputAttackVector : -lastEnemyVector);

                    // 상태 변경
                    SetState(State.Attack);
                }
                else
                {
                    // 애니메이션
                    animator.SetBool("Moving", moveDirection != Vector3.zero);

                    if (Managers.Monster.GetChasingMonsterCount() <= 0)
                    {
                        // 체력 풀충전
                        if (fullHealDuration != 0)
                        {
                            var healAmount = PlayerDataManager.Instance.Model.MaxStat.hp * (Time.deltaTime / fullHealDuration);
                            healAmount = Mathf.Ceil(healAmount);
                            PlayerDataManager.Instance.GainHp((long)healAmount);
                        }
                    }
                }
            }
            else if (nowState == State.Attack)
            {
                if (Time.time > attackEnd)
                    SetState(State.Idle);
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

        public void SetState(State state)
        {
            nowState = state;
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

        void LevelUpEffect(long level, long prevLevel)
        {
            if (level > prevLevel && PoolManager.Instance.TryCreate("Prefab/Effect/HCFX_Energy_08", out var effect))
            {
                effect.transform.parent = transform;
                effect.transform.position = transform.position + new Vector3(0f, -1f, 0f);
            }
        }
    }
}
