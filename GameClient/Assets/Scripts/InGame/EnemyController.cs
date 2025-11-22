using GameData;
using GameUI;
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

        private void Awake()
        {
            unitModel = new UnitModel("Lv. 12", new Stat() { attack = 2, hp = 20 });
        }

        private void Start()
        {
            nowState = State.Idle;
        }

        private void OnEnable()
        {
            Managers.Monster?.enemyControllers.Add(this);
            Managers.UI?.hudLayout.RegisterNameTag(unitModel.name, transform);
            unitModel.Reset();
        }

        private void OnDisable()
        {
            Managers.Monster?.enemyControllers.Remove(this);
            Managers.UI?.hudLayout.RemoveHpGuage(transform);
            Managers.UI?.hudLayout.RemoveNameTag(transform);
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
            // 데미지 적용
            var damage = 5L;
            unitModel.TakeDamage(damage);

            // 피격 연출
            rigid.linearVelocity = new Vector3(0f, rigid.linearVelocity.y, 0f);
            rigid.rotation = Quaternion.LookRotation(-pushed);
            rigid.AddForce(pushed.normalized * knockBack, ForceMode.Impulse);
            targetPlayer = attacker;

            nowState = State.Damaged;
            knockBackEnd = DateTime.Now.AddSeconds(knockBackTime);

            // 피격 효과
            if (PoolManager.Instance.TryCreate("Prefab/Effect/HCFX_Hit_08", out var go))
                go.transform.position = transform.position + new Vector3(0f, 1f, 0f);

            // 체력바
            if (!unitModel.IsDead())
            {
                Managers.UI?.hudLayout.RegisterHpGuage(transform);
                Managers.UI?.hudLayout.UpdateHpGuageValue(transform, unitModel.MaxStat.hp, unitModel.NowStat.hp);
            }

            // 데미지바
            Managers.UI?.hudLayout.ShowDamage(new long[] { damage }, transform.position + new Vector3(0f, 1.8f, 0f));

            // 사망 적용
            if (unitModel.IsDead())
                OnDead();
        }

        public void OnDead()
        {
            // 사망 이펙트
            if (PoolManager.Instance.TryCreate("Prefab/Effect/CFXR2 WW Enemy Explosion", out var go))
                go.transform.position = transform.position + new Vector3(0f, 1f, 0f);

            // 코인 분사
            for (int i = 0; i < 5; i++)
            {
                if (PoolManager.Instance.TryCreate("Prefab/Item/Coin", out var coin))
                {
                    coin.transform.position = transform.position + new Vector3(0f, 1f, 0f);
                    coin.GetComponent<DropItemEffect>().DropItem(transform.position + new Vector3(0f, 1f, 0f), targetPlayer.transform);
                }
                    
            }

            // 초기화
            gameObject.SetActive(false);
            targetPlayer = null;
            GetComponent<Poolable>().ReleaseSelf();
        }
    }
}
