using UnityEngine;

namespace AutoBattler.Battle
{
    /// <summary>
    /// BattleUnit이 직접 Animator 파라미터 이름을 알 필요 없도록 만든 어댑터.
    /// 유닛 프리팹에 Animator와 함께 부착.
    ///
    /// 현재 사용 중인 Animator Controller 규약 (인스펙터에서 변경 가능):
    ///   - Bool    "isWalking" : 이동 중
    ///   - Trigger "attack"    : 기본 공격
    ///
    /// 아래 useXxx 플래그로 토글 가능한 옵션 파라미터:
    ///   - Trigger "skill", "hit"
    ///   - Bool    "isDead"
    /// Animator에 해당 파라미터가 추가되면 인스펙터에서 켜기.
    /// </summary>
    [DisallowMultipleComponent]
    public class UnitAnimator : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        [Header("파라미터 이름")]
        [SerializeField] private string walkBoolName = "isWalking";
        [SerializeField] private string attackTriggerName = "attack";
        [SerializeField] private string skillTriggerName = "skill";
        [SerializeField] private string hitTriggerName = "hit";
        [SerializeField] private string deadBoolName = "isDead";

        [Header("Animator에 존재하는 파라미터만 켜기")]
        [SerializeField] private bool useWalk = true;
        [SerializeField] private bool useAttack = true;
        [SerializeField] private bool useSkill = false;
        [SerializeField] private bool useHit = false;
        [SerializeField] private bool useDead = false;

        // 해시 캐싱 (문자열 비교 비용 제거)
        private int _hWalk, _hAttack, _hSkill, _hHit, _hDead;

        private void Reset() { animator = GetComponentInChildren<Animator>(); }

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            _hWalk = Animator.StringToHash(walkBoolName);
            _hAttack = Animator.StringToHash(attackTriggerName);
            _hSkill = Animator.StringToHash(skillTriggerName);
            _hHit = Animator.StringToHash(hitTriggerName);
            _hDead = Animator.StringToHash(deadBoolName);
        }

        public void SetMoving(bool moving)
        {
            if (!useWalk || animator == null) return;
            animator.SetBool(_hWalk, moving);
        }

        public void PlayAttack()
        {
            if (!useAttack || animator == null) return;
            animator.SetTrigger(_hAttack);
        }

        public void PlaySkill()
        {
            if (!useSkill || animator == null) return;
            animator.SetTrigger(_hSkill);
        }

        public void PlayHit()
        {
            if (!useHit || animator == null) return;
            animator.SetTrigger(_hHit);
        }

        public void PlayDeath()
        {
            if (!useDead || animator == null) return;
            animator.SetBool(_hDead, true);
        }

        public void Revive()
        {
            if (!useDead || animator == null) return;
            animator.SetBool(_hDead, false);
        }
    }
}