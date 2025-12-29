using InGameModel;
using UnityEngine;

namespace InGame
{
    public class BaseUnit : MonoBehaviour
    {
        // Settings
        public int TeamId {  get; set; }

        public float detectRange;
        public float attackRange;
        public float moveSpeed = 5f;
        public float attackDuration; // 이게 끝나야 데미지 들어감
        public float attackDelay;

        protected UnitModel model;
        protected float attackAngleCos = 0.707f;

        // Values
        protected Vector2 attackDir;

        protected float attackEnd;
        protected float attackDelayEnd;

        public void SetModel(UnitModel model)
        {
            this.model = model;
        }

        public void OnSpawn()
        {
            model.Spawn();
        }

        public void OnDamage(long damage)
        {
            model.OnDamage(damage);

            if (!model.IsAlive())
                OnDead();
        }

        public virtual void OnDead()
        {

        }

        public bool IsAlive()
        {
            return model.IsAlive();
        }

        public void CancelAttack()
        {
            CancelInvoke(nameof(OnAttackFinish));
        }

        protected void AttackTarget()
        {
            if (!IsAlive() || !IsAttackDelayEnd())
                return;

            attackEnd = Time.time + attackDuration;
            attackDelayEnd = Time.time + attackDelay;

            // 공격 끝나고 데미지 들어감
            Invoke(nameof(OnAttackFinish), attackDuration);
        }

        protected bool IsAttacking()
        {
            return Time.time < attackEnd;
        }

        protected bool IsAttackDelayEnd()
        {
            return Time.time >= attackDelayEnd;
        }

        protected bool IsTargetInAttackRange()
        {
            var detects = Physics2D.OverlapCircleAll(transform.position, attackRange);
            foreach (var detect in detects)
            {
                var unit = detect.GetComponent<BaseUnit>();
                if (unit != null)
                {
                    if (unit.TeamId != TeamId && unit.IsAlive())
                    {
                        attackDir = (unit.transform.position - transform.position).normalized;
                        return true;
                    }
                }
            }

            return false;
        }

        public bool IsDetectEnemy(out BaseUnit enemy)
        {
            var detects = Physics2D.OverlapCircleAll(transform.position, detectRange);
            foreach (var detect in detects)
            {
                var unit = detect.GetComponent<BaseUnit>();
                if (unit != null)
                {
                    if (unit.TeamId != TeamId && unit.IsAlive())
                    {
                        enemy = unit;
                        return true;
                    }
                }
            }

            enemy = null;
            return false;
        }

        void OnAttackFinish()
        {
            var detects = Physics2D.OverlapCircleAll(transform.position, attackRange);
            foreach (var detect in detects)
            {
                var unit = detect.GetComponent<BaseUnit>();
                if (unit != null)
                {
                    // 적이고 살아있음
                    if (unit.TeamId != TeamId && unit.IsAlive())
                    {
                        // 공격 각도 체크
                        if (IsInRange(attackDir, unit.transform.position - transform.position, attackAngleCos))
                        {
                            unit.OnDamage(model.attack);
                            return;
                        }
                    }
                }
            }
        }

        // 공격 각도 범위 체크 코드. fanCos는 코사인 값. (시계, 반시계 45도씩이면 cos45 값 입력)
        bool IsInRange(Vector2 attackDir, Vector2 targetDir, float fanCos)
        {
            var dot = Vector2.Dot(attackDir.normalized, targetDir.normalized);
            return dot > fanCos;
        }

        // 에디터에서 범위를 보기 위한 기즈모
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, detectRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)attackDir * attackRange);
        }
    }
}
