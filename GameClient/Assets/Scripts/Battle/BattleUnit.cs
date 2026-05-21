using System.Collections.Generic;
using UnityEngine;
using AutoBattler.Core;
using AutoBattler.Data;
using AutoBattler.Heroes;

namespace AutoBattler.Battle
{
    /// <summary>
    /// 전투에서 실제로 움직이는 단위.
    /// 영웅/적 공통. 위치는 그리드 셀, 월드 좌표는 BattleField가 보간/배치.
    ///
    /// AI 규칙(사양 그대로):
    ///   - 가장 가까운 적에게 이동
    ///   - 같은 거리면 랜덤
    ///   - 사정거리 안 들어오면 1초에 1칸 이동
    ///   - 사정거리 들어오면 정지 후 공격
    ///   - 공격속도 100 = 1초 1회
    /// </summary>
    public class BattleUnit : MonoBehaviour
    {
        // --- 정체성 ---
        public Team Team { get; private set; }
        public string DisplayName { get; private set; }
        public Hero SourceHero { get; private set; }   // 적이면 null

        // --- 위치 ---
        public Vector2Int Cell;                         // 현재(점유) 셀
        public bool IsAlive => CurrentHP > 0f;

        /// <summary>BattleField가 매 프레임 Slerp로 따라가는 목표 바라보기 방향 (XZ 평면).</summary>
        public Vector3 DesiredFacing = Vector3.forward;

        // 연속 이동 상태
        private bool _isMoving;          // 한 칸 이동 진행 중
        private Vector3 _moveStartWorld;    // 이동 시작 월드 좌표
        private Vector3 _moveTargetWorld;   // 이동 목표 월드 좌표
        private float _moveDuration;      // 한 칸 이동에 걸리는 시간(초)
        private float _moveElapsed;       // 경과 시간

        // --- 스탯/전투 ---
        public Stats Stats { get; private set; }
        public float CurrentHP { get; private set; }
        public int AttackRange { get; private set; }
        public WeaponData Weapon { get; private set; }

        // 스킬
        private readonly List<SkillData> _skills = new List<SkillData>(2);
        private readonly List<float> _cooldownRemain = new List<float>(2);

        // 행동 타이머
        private float _attackCooldown;   // 다음 공격까지 남은 시간

        // 타깃 락온 — 매 프레임 새로 안 뽑고 유지하다가 무효해지면 교체
        private BattleUnit _currentTarget;

        // 시각 컴포넌트
        [SerializeField] private UnitAnimator anim;   // 프리팹에 부착 (선택)
        private bool _wasMoving;                       // MoveSpeed 파라미터 변경 감지용

        // 의존성 주입
        private BattleField _field;

        // ─────────────────────────────────────────────────────────
        // 초기화
        // ─────────────────────────────────────────────────────────
        public void InitAsHero(Hero hero, BattleField field, Team team)
        {
            SourceHero = hero;
            DisplayName = hero.data.displayName;
            Weapon = null;   // 무기는 직업이 결정 (시각/모션 측면). 전투 수치는 Stats로 합산됨.
            Stats = hero.GetFinalStats();
            CurrentHP = Stats.maxHp;
            AttackRange = hero.GetBaseAttackRange();

            _skills.Clear();
            _cooldownRemain.Clear();
            if (hero.skillA != null) { _skills.Add(hero.skillA); _cooldownRemain.Add(0f); }
            if (hero.skillB != null) { _skills.Add(hero.skillB); _cooldownRemain.Add(0f); }

            Init(field, team);
        }

        public void InitAsEnemy(string name, Stats stats, WeaponData weapon,
                                IList<SkillData> skills, BattleField field)
        {
            DisplayName = name;
            Stats = stats;
            CurrentHP = stats.maxHp;
            Weapon = weapon;
            AttackRange = weapon != null
                ? Mathf.Max(1, weapon.baseAttackRange + stats.attackRange)
                : Mathf.Max(1, 1 + stats.attackRange);

            _skills.Clear();
            _cooldownRemain.Clear();
            if (skills != null)
                foreach (var s in skills)
                {
                    _skills.Add(s);
                    _cooldownRemain.Add(0f);
                }

            Init(field, Team.Enemy);
        }

        private void Init(BattleField field, Team team)
        {
            _field = field;
            Team = team;
            _attackCooldown = Stats.AttackInterval; // 첫 공격에 약간의 텀

            // 이동 상태 초기화
            _isMoving = false;
            _moveElapsed = 0f;
            _currentTarget = null;

            // 풀 재사용 시 이전 상태 청소
            CancelInvoke(nameof(HideAfterDeath));
            if (!gameObject.activeSelf) gameObject.SetActive(true);

            if (anim == null) anim = GetComponent<UnitAnimator>();
            if (anim == null) anim = GetComponentInChildren<UnitAnimator>();
            anim?.SetMoving(false);
            anim?.Revive();
            _wasMoving = false;
        }

        // ─────────────────────────────────────────────────────────
        // 메인 루프 — BattleField에서 Tick 호출
        // ─────────────────────────────────────────────────────────
        public void Tick(float dt, List<BattleUnit> allUnits)
        {
            if (!IsAlive) return;

            // 쿨다운 감소
            _attackCooldown = Mathf.Max(0f, _attackCooldown - dt);
            for (int i = 0; i < _cooldownRemain.Count; i++)
                _cooldownRemain[i] = Mathf.Max(0f, _cooldownRemain[i] - dt);

            // 1) 이동 중이면 보간 진행 (도착 전엔 다른 행동 안 함 — 칸 단위 이동 사양 유지)
            if (_isMoving)
            {
                _moveElapsed += dt;
                float t = _moveDuration <= 0f ? 1f : Mathf.Clamp01(_moveElapsed / _moveDuration);
                transform.position = Vector3.Lerp(_moveStartWorld, _moveTargetWorld, t);

                if (t >= 1f)
                {
                    // 도착
                    _isMoving = false;
                    _moveElapsed = 0f;
                    transform.position = _moveTargetWorld; // 스냅 (오차 누적 방지)
                }
                else
                {
                    SetMoving(true);
                    // 이동 중엔 진행 방향을 봄 (월드 좌표 차이 = 진행 방향)
                    Vector3 dir = _moveTargetWorld - _moveStartWorld;
                    dir.y = 0;
                    if (dir.sqrMagnitude > 0.0001f) DesiredFacing = dir.normalized;
                    return;
                }
            }

            // 2) 타깃 탐색
            var target = AcquireTarget(allUnits);
            if (target == null) { SetMoving(false); return; }

            UpdateFacing(target.Cell);

            // 3) 스킬 우선
            if (TryCastReadySkill(target, allUnits))
            {
                SetMoving(false);
                anim?.PlaySkill();
                return;
            }

            // 4) 사거리 안이면 기본 공격
            //    range==1 → 맨해튼 (대각 불가)
            //    range>=2 → 체비셰프 (대각 포함)
            if (BattleGrid.InAttackRange(Cell, target.Cell, AttackRange))
            {
                SetMoving(false);
                if (_attackCooldown <= 0f)
                {
                    DoBasicAttack(target);
                    anim?.PlayAttack();
                    _attackCooldown = Stats.AttackInterval;
                }
                return;
            }

            // 5) 사거리 밖이면 다음 칸 예약 + 이동 시작
            var next = _field.Grid.NextStepToward(this, target.Cell);
            if (next.HasValue)
            {
                BeginMoveTo(next.Value);
                SetMoving(true);
            }
            else
            {
                // 막혀있으면 대기
                SetMoving(false);
            }
        }

        /// <summary>
        /// 한 칸 이동 시작. Cell(논리 위치)은 즉시 다음 칸으로 옮겨 점유를 선점하고,
        /// 시각적 위치는 _moveDuration 동안 보간된다.
        /// </summary>
        private void BeginMoveTo(Vector2Int nextCell)
        {
            _moveStartWorld = _field.CellToWorld(Cell);
            _moveTargetWorld = _field.CellToWorld(nextCell);
            _field.Grid.TryMove(this, nextCell);   // Cell이 nextCell로 갱신됨

            // 이동 시간: moveSpeed 1.0 = 1초/칸 (사양 그대로)
            _moveDuration = Stats.moveSpeed <= 0f ? 99f : 1f / Stats.moveSpeed;
            _moveElapsed = 0f;
            _isMoving = true;
        }

        /// <summary>
        /// 타깃 락온 로직.
        ///   - 현재 타깃이 살아있고 사거리 안 → 계속 유지 (안정적 응시/공격)
        ///   - 현재 타깃이 죽었거나 사거리 밖 → 새로 가까운 적 찾기
        /// </summary>
        private BattleUnit AcquireTarget(List<BattleUnit> allUnits)
        {
            // 현재 타깃이 여전히 유효(살아있고 적이고 사거리 안)이면 유지
            if (_currentTarget != null
                && _currentTarget.IsAlive
                && _currentTarget.Team != Team
                && BattleGrid.InAttackRange(Cell, _currentTarget.Cell, AttackRange))
                return _currentTarget;

            // 사거리 밖이거나 죽었으면 가장 가까운 적으로 갱신
            _currentTarget = _field.Grid.FindNearestEnemy(this, allUnits);
            return _currentTarget;
        }

        private void UpdateFacing(Vector2Int targetCell)
        {
            // 그리드 y는 월드 z에 매핑 (BattleField.CellToWorld 와 일치)
            Vector3 dir = new Vector3(targetCell.x - Cell.x, 0f, targetCell.y - Cell.y);
            if (dir.sqrMagnitude > 0.0001f) DesiredFacing = dir.normalized;
        }

        private void SetMoving(bool moving)
        {
            if (moving == _wasMoving) return;
            _wasMoving = moving;
            anim?.SetMoving(moving);
        }

        /// <summary>전투 종료 시 BattleField가 호출. 이동 모션/이동 보간 정지.</summary>
        public void StopVisuals()
        {
            _isMoving = false;
            _moveElapsed = 0f;
            SetMoving(false);
        }

        // ─────────────────────────────────────────────────────────
        // 행동
        // ─────────────────────────────────────────────────────────
        private void DoBasicAttack(BattleUnit target)
        {
            float power = Stats.attack
                          * (Weapon != null ? Weapon.baseAttackPowerMul : 1f);
            DealDamage(this, target, power);
        }

        private bool TryCastReadySkill(BattleUnit primaryTarget, List<BattleUnit> all)
        {
            for (int i = 0; i < _skills.Count; i++)
            {
                if (_cooldownRemain[i] > 0f) continue;
                var s = _skills[i];
                if (s == null) continue;
                if (!BattleGrid.InAttackRange(Cell, primaryTarget.Cell, s.range)) continue;

                SkillExecutor.Execute(this, s, primaryTarget, all, _field);
                _cooldownRemain[i] = s.cooldown;
                return true; // 한 틱에 한 스킬만
            }
            return false;
        }

        // ─────────────────────────────────────────────────────────
        // 데미지 처리 (공용)
        // ─────────────────────────────────────────────────────────
        public static void DealDamage(BattleUnit attacker, BattleUnit target, float power)
        {
            if (target == null || !target.IsAlive) return;

            bool crit = Random.value < attacker.Stats.critRate;
            float multiplier = crit ? Mathf.Max(1f, attacker.Stats.critDamage) : 1f;

            // 간단한 방어 공식: 방어력 1당 1% 감쇠, 최대 75%
            float reduction = Mathf.Clamp(target.Stats.defense * 0.01f, 0f, 0.75f);
            float dmg = power * multiplier * (1f - reduction);
            dmg = Mathf.Max(1f, dmg);

            target.CurrentHP -= dmg;
            if (target.CurrentHP <= 0f) target.OnDeath();
            else target.anim?.PlayHit();
        }

        public void Heal(float amount)
        {
            if (!IsAlive) return;
            CurrentHP = Mathf.Min(Stats.maxHp, CurrentHP + amount);
        }

        [Header("사망 처리")]
        [SerializeField] private float deathHideDelay = 1.5f; // Death 애니 재생 후 숨김 시간

        private void OnDeath()
        {
            CurrentHP = 0f;
            _field?.Grid.Remove(this);          // 그리드는 즉시 비움 (다른 유닛 진로 방해 X)
            anim?.SetMoving(false);
            anim?.PlayDeath();
            // 시각적으로는 잠깐 시체 보이고 사라짐
            Invoke(nameof(HideAfterDeath), deathHideDelay);
        }

        private void HideAfterDeath()
        {
            // 풀링이면 반납, 아니면 비활성화
            var poolable = GetComponent<Poolable>();
            if (poolable != null && PoolManager.Instance != null)
                poolable.ReleaseSelf();
            else
                gameObject.SetActive(false);
        }
    }
}