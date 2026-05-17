using System;
using System.Collections.Generic;
using UnityEngine;
using AutoBattler.Core;
using AutoBattler.Data;
using AutoBattler.Heroes;
using AutoBattler.Battle;

namespace AutoBattler.Rounds
{
    /// <summary>
    /// 한 런(30~45분, 15라운드)을 관리.
    /// 흐름:
    ///   StartNewRun -> StartNextRound -> [전투]
    ///     -> 승리: OnRewardOffered(스킬 N개 자동 지급) -> ConfirmRewards()
    ///                                                  -> OnLoadoutReady (장착/합성 화면)
    ///                                                  -> ProceedToNextRound() -> StartNextRound
    ///     -> 패배: OnRunFailed
    ///   15라운드 클리어 후: OnRunCompleted
    /// </summary>
    public class RunManager : MonoBehaviour
    {
        public const int MaxHeroes = 5;
        public const int TotalRounds = 15;

        [Header("씬 참조")]
        public BattleField battleField;

        [Header("기본 풀 (디자이너가 채움)")]
        public HeroData[] startingHeroPool;
        public HeroData[] recruitablePool;
        public WeaponData[] weaponPool;
        public EquipmentData[] equipmentPool;
        public SkillData[] skillPool;

        [Header("보상 규칙")]
        [Tooltip("라운드당 최소 보상 스킬 수")]
        public int minRewardSkills = 2;
        [Tooltip("라운드당 최대 보상 스킬 수")]
        public int maxRewardSkills = 4;

        // ─────────────────────────────────────────────────────────
        // 런 상태
        // ─────────────────────────────────────────────────────────
        public List<Hero> Roster { get; } = new List<Hero>();
        public int CurrentRound { get; private set; } = 0;
        public bool IsRunOver { get; private set; }

        public List<WeaponData> InventoryWeapons { get; } = new List<WeaponData>();
        public List<EquipmentData> InventoryEquipment { get; } = new List<EquipmentData>();
        public SkillInventory SkillInv { get; } = new SkillInventory();

        /// <summary>영웅별 그리드 셀 배치 (아군 영역: y=0..3)</summary>
        public Dictionary<Hero, Vector2Int> Placement { get; } = new Dictionary<Hero, Vector2Int>();

        // ─────────────────────────────────────────────────────────
        // 이벤트 — UI 가 구독
        // ─────────────────────────────────────────────────────────
        public event Action<int> OnRoundStarted;
        public event Action<int, bool> OnRoundEnded;
        public event Action<List<SkillData>> OnRewardOffered;
        public event Action OnLoadoutReady;        // 보상 확인 후 장착 화면
        public event Action OnPlacementReady;      // 장착 확인 후 배치 화면
        public event Action OnRunCompleted;
        public event Action OnRunFailed;

        private List<SkillData> _lastRewardSkills;

        // ─────────────────────────────────────────────────────────
        // 런 시작
        // ─────────────────────────────────────────────────────────
        public void StartNewRun(HeroData[] startingPick)
        {
            Roster.Clear();
            InventoryWeapons.Clear();
            InventoryEquipment.Clear();
            SkillInv.Clear();
            Placement.Clear();
            CurrentRound = 0;
            IsRunOver = false;

            // 2명으로 시작
            int n = Mathf.Min(2, startingPick.Length);
            for (int i = 0; i < n; i++) Roster.Add(new Hero(startingPick[i]));

            // 초기 자동 배치 (앞줄부터)
            AutoPlaceMissingHeroes();

            if (battleField != null)
            {
                battleField.OnBattleEnded -= OnBattleResult;
                battleField.OnBattleEnded += OnBattleResult;
            }

            // 첫 라운드 시작 전에도 배치 단계 거침
            OnPlacementReady?.Invoke();
        }

        public bool TryRecruitHero(HeroData data)
        {
            if (Roster.Count >= MaxHeroes) return false;
            Roster.Add(new Hero(data));
            AutoPlaceMissingHeroes(); // 신규 영웅은 빈 칸 자동 배치
            return true;
        }

        /// <summary>아직 배치 안 된 영웅이 있으면 앞줄부터 빈 칸에 자동 배치.</summary>
        private void AutoPlaceMissingHeroes()
        {
            foreach (var hero in Roster)
            {
                if (Placement.ContainsKey(hero)) continue;

                // 앞줄부터 빈 칸 찾기 (y=0..3, x=0..4)
                bool placed = false;
                for (int y = 0; y <= BattleGrid.AllyZoneMaxY && !placed; y++)
                    for (int x = 0; x < BattleGrid.Width && !placed; x++)
                    {
                        var cell = new Vector2Int(x, y);
                        if (!Placement.ContainsValue(cell))
                        {
                            Placement[hero] = cell;
                            placed = true;
                        }
                    }
            }
        }

        /// <summary>외부(배치 UI)에서 호출: 영웅을 특정 셀에. 이미 있으면 swap.</summary>
        public void SetPlacement(Hero hero, Vector2Int cell)
        {
            if (hero == null) return;
            if (cell.y < 0 || cell.y > BattleGrid.AllyZoneMaxY) return;
            if (cell.x < 0 || cell.x >= BattleGrid.Width) return;

            // 그 셀에 이미 있는 다른 영웅
            Hero occupant = null;
            foreach (var kv in Placement)
                if (kv.Value == cell && kv.Key != hero) { occupant = kv.Key; break; }

            if (occupant != null)
            {
                // swap: 점유자를 hero의 기존 자리로
                if (Placement.TryGetValue(hero, out var oldCell))
                    Placement[occupant] = oldCell;
                else
                    Placement.Remove(occupant); // hero가 아직 배치 안 됐던 케이스
            }
            Placement[hero] = cell;
        }

        // ─────────────────────────────────────────────────────────
        // 라운드 흐름
        // ─────────────────────────────────────────────────────────
        public void StartNextRound()
        {
            if (IsRunOver) return;
            CurrentRound++;
            if (CurrentRound > TotalRounds)
            {
                IsRunOver = true;
                OnRunCompleted?.Invoke();
                return;
            }

            OnRoundStarted?.Invoke(CurrentRound);
            var enemies = BuildEnemiesForRound(CurrentRound);
            battleField.StartBattle(Roster, Placement, enemies);
        }

        private void OnBattleResult(bool won)
        {
            OnRoundEnded?.Invoke(CurrentRound, won);

            if (!won)
            {
                IsRunOver = true;
                AutoSave();
                OnRunFailed?.Invoke();
                return;
            }

            // 스킬 자동 지급
            _lastRewardSkills = RollRewardSkills(CurrentRound);
            foreach (var s in _lastRewardSkills) SkillInv.Add(s, 1);

            AutoSave();
            OnRewardOffered?.Invoke(_lastRewardSkills);
        }

        /// <summary>보상 화면의 "확인" 버튼이 호출. 장착/합성 화면을 열라는 신호.</summary>
        public void ConfirmRewards()
        {
            _lastRewardSkills = null;
            OnLoadoutReady?.Invoke();
        }

        /// <summary>장착/합성 화면의 "확인" 버튼이 호출. 배치 화면 열라는 신호.</summary>
        public void ConfirmLoadout()
        {
            OnPlacementReady?.Invoke();
        }

        /// <summary>배치 화면의 "전투 시작" 버튼이 호출. 다음 라운드 시작.</summary>
        public void ProceedToNextRound()
        {
            StartNextRound();
        }

        // ─────────────────────────────────────────────────────────
        // 스킬 장착 — UI에서 호출
        // ─────────────────────────────────────────────────────────
        public enum LoadoutSlot { A, B }

        /// <summary>
        /// 인벤토리의 스킬을 영웅의 슬롯에 장착.
        /// 슬롯이 차있으면 기존 스킬은 인벤토리로 복귀.
        /// 전투 중에는 사용 금지 (BattleField.State 검사는 UI 측에서).
        /// </summary>
        public bool EquipSkill(Hero hero, SkillData skill, LoadoutSlot slot)
        {
            if (hero == null || skill == null) return false;
            if (SkillInv.CountOf(skill) <= 0) return false;

            // 같은 스킬을 같은 슬롯에 또 넣으려는 시도 무시
            var curInSlot = slot == LoadoutSlot.A ? hero.skillA : hero.skillB;
            if (curInSlot == skill) return false;

            // 인벤토리에서 빼서
            SkillInv.Remove(skill, 1);

            // 기존 슬롯 스킬은 인벤토리로 복귀
            if (curInSlot != null) SkillInv.Add(curInSlot, 1);

            // 장착
            if (slot == LoadoutSlot.A) hero.skillA = skill;
            else hero.skillB = skill;

            return true;
        }

        /// <summary>슬롯에서 인벤토리로 되돌림.</summary>
        public bool UnequipSkill(Hero hero, LoadoutSlot slot)
        {
            if (hero == null) return false;
            var cur = slot == LoadoutSlot.A ? hero.skillA : hero.skillB;
            if (cur == null) return false;

            if (slot == LoadoutSlot.A) hero.skillA = null;
            else hero.skillB = null;
            SkillInv.Add(cur, 1);
            return true;
        }

        // ─────────────────────────────────────────────────────────
        // 콘텐츠 빌더
        // ─────────────────────────────────────────────────────────
        protected virtual List<SkillData> RollRewardSkills(int round)
        {
            var list = new List<SkillData>();
            if (skillPool == null || skillPool.Length == 0) return list;

            int count = UnityEngine.Random.Range(minRewardSkills, maxRewardSkills + 1); // 2~4
            for (int i = 0; i < count; i++)
            {
                var s = skillPool[UnityEngine.Random.Range(0, skillPool.Length)];
                list.Add(s); // 중복 허용
            }
            return list;
        }

        protected virtual List<EnemySpawn> BuildEnemiesForRound(int round)
        {
            var list = new List<EnemySpawn>();
            int count = Mathf.Clamp(2 + round / 2, 2, 8);
            float hpScale = 1f + (round - 1) * 0.15f;
            float atkScale = 1f + (round - 1) * 0.10f;

            for (int i = 0; i < count; i++)
            {
                list.Add(new EnemySpawn
                {
                    name = $"Enemy_{round}_{i}",
                    cell = new Vector2Int(i % BattleGrid.Width,
                                          BattleGrid.Height - 1 - (i / BattleGrid.Width)),
                    stats = new Stats
                    {
                        attack = 8 * atkScale,
                        defense = 1,
                        maxHp = 60 * hpScale,
                        critRate = 0.05f,
                        critDamage = 1.5f,
                        attackSpeed = 100,
                        range = 0,
                        moveSpeed = 1f
                    },
                    weapon = null,
                    skills = null
                });
            }
            return list;
        }

        // ─────────────────────────────────────────────────────────
        // 저장 훅
        // ─────────────────────────────────────────────────────────
        private void AutoSave()
        {
            // TODO: 직렬화 연결
            Debug.Log($"[AutoSave] round={CurrentRound}, heroes={Roster.Count}, skillKinds={SkillInv.Counts.Count}");
        }
    }
}