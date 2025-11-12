using GameData;
using System;
using UnityEngine;

namespace InGame
{
    public class EnemyController : MonoBehaviour
    {
        UnitModel unitModel;

        public Rigidbody rigid;
        public TriggerEvent detectTrigger;

        public float knockBack;
        public float knockBackTime;
        public float moveSpeed;
        public float chaseDistance;
        public float targetPositionError;

        public GameObject targetPlayer;
        State nowState;
        DateTime knockBackEnd;
        public Vector3 targetPosition;

        enum State
        {
            Idle,
            Move,
            Damaged
        }

        private void Start()
        {
            unitModel = new UnitModel("한나라 병사", new Stat() { attack = 2, hp = 20 });
            nowState = State.Idle;
        }

        private void OnEnable()
        {
            Managers.Instance.GetComponent<MonsterManager>().activeMonsterCount++;
        }

        private void OnDisable()
        {
            Managers.Instance.GetComponent<MonsterManager>().activeMonsterCount--;
        }

        private void Update()
        {
            if (nowState == State.Idle)
            {
                if (targetPlayer != null)
                    nowState = State.Move;
            }
            else if (nowState == State.Move)
            {
                if (targetPlayer != null)
                {
                    var targetDistance = (targetPlayer.transform.position - transform.position).sqrMagnitude;
                    if (targetDistance < chaseDistance)
                    {
                        targetPosition = targetPlayer.transform.position;
                    }
                    else
                    {
                        targetPlayer = null;
                        nowState = State.Idle;
                    }
                }
                else
                {
                    nowState = State.Idle;
                }
            }
            else if (nowState == State.Damaged)
            {
                if (DateTime.Now > knockBackEnd)
                    nowState = State.Idle;
            }
        }

        private void FixedUpdate()
        {
            if (nowState == State.Move)
            {
                var deltaPosition = targetPosition - transform.position;

                if (deltaPosition.sqrMagnitude > targetPositionError)
                {
                    var resultVelocity = deltaPosition.normalized * moveSpeed;
                    resultVelocity.y = rigid.linearVelocity.y;
                    deltaPosition.y = 0f;

                    rigid.linearVelocity = resultVelocity;
                    rigid.rotation = Quaternion.LookRotation(deltaPosition);
                }
            }
        }

        public void OnAttacked(Vector3 pushed, GameObject attacker)
        {
            rigid.linearVelocity = new Vector3(0f, rigid.linearVelocity.y, 0f);
            rigid.rotation = Quaternion.LookRotation(-pushed);
            rigid.AddForce(pushed.normalized * knockBack, ForceMode.Impulse);
            targetPlayer = attacker;

            nowState = State.Damaged;
            knockBackEnd = DateTime.Now.AddSeconds(knockBackTime);

            if (Managers.Instance.GetComponent<GameObjectManager>().TryCreate("Effect/HCFX_Hit_08", out var go))
                go.transform.position = transform.position + new Vector3(0f, 1f, 0f);
        }

        public void OnDead()
        {
            gameObject.SetActive(false);
            targetPlayer = null;
        }
    }
}
