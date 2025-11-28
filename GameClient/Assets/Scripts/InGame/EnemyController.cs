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

        private void Awake()
        {
            unitModel = new UnitModel($"TEST_MOB", StatBalancer.GetMonsterStatsByLevel(0));
        }

        private void Start()
        {
            nowState = State.Idle;
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
                        unitModel.Reset();
                        Managers.UI?.hudLayout.RemoveHpGuage(transform);
                        ChasePlayer(false);
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
        
        public void SetLevel(int level)
        {
            unitModel.SetStat(StatBalancer.GetMonsterStatsByLevel(level));
            unitModel.Level = level;
            unitModel.Reset();
            Managers.Monster?.enemyControllers.Add(this);
            Managers.UI?.hudLayout.RegisterNameTag($"Lv. {unitModel.Level}", transform);
        }

        public void OnAttacked(Vector3 pushed, GameObject attacker)
        {
            // 데미지 적용
            var damage = BattleCalculator.GetDamage(PlayerDataManager.Instance.Model.GetAttack(), unitModel.GetDefense());
            unitModel.TakeDamage(damage);
            Debug.Log($"{PlayerDataManager.Instance.Model.GetAttack()}, {unitModel.GetDefense()}");

            // 피격 연출
            rigid.linearVelocity = new Vector3(0f, rigid.linearVelocity.y, 0f);
            rigid.rotation = Quaternion.LookRotation(-pushed);
            rigid.AddForce(pushed.normalized * knockBack, ForceMode.Impulse);
            ChasePlayer(true, attacker);

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

            // 유저에게 반격
            PlayerDataManager.Instance.TakeDamage(5);
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

            // 유저 경험치 및 골드 증가
            PlayerDataManager.Instance?.GainExp(StatBalancer.GetMonsterExpByLevel(unitModel.Level));
            PlayerDataManager.Instance?.GainMoney(StatBalancer.GetMonsterGoldByLevel(unitModel.Level), false);

            // 초기화
            gameObject.SetActive(false);
            ChasePlayer(false);
            GetComponent<Poolable>().ReleaseSelf();
        }

        void ChasePlayer(bool chase, GameObject target = null)
        {
            if (chase)
            {
                targetPlayer = target;
                Managers.Monster.chasingEnemies.Add(this);
            }
            else
            {
                targetPlayer = null;
                Managers.Monster.chasingEnemies.Remove(this);
            }
        }
    }
}
