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
    /// - 시작: 2명 영웅, 최대 5명까지 영입
    /// - 라운드마다 전투 → 보상 1택 → 다음 라운드
    /// - 라운드 단위 자동 저장 훅(SaveSystem 연결 지점 표시)
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

        // ─────────────────────────────────────────────────────────
        // 런 상태
        // ─────────────────────────────────────────────────────────
        public List<Hero> Roster { get; } = new List<Hero>();
        public int CurrentRound { get; private set; } = 0; // 0 = 미시작
        public bool IsRunOver { get; private set; }

        // 인벤토리(보유 자원)
        public List<WeaponData> InventoryWeapons { get; } = new List<WeaponData>();
        public List<EquipmentData> InventoryEquipment { get; } = new List<EquipmentData>();
        public Dictionary<SkillData, int> SkillBookCounts { get; } = new Dictionary<SkillData, int>();

        // 이벤트
        public event Action<int> OnRoundStarted;
        public event Action<int, bool> OnRoundEnded;      // round, won
        public event Action<List<RewardOption>> OnRewardOffered;
        public event Action OnRunCompleted;
        public event Action OnRunFailed;

        private List<RewardOption> _pendingRewards;

        // ─────────────────────────────────────────────────────────
        // 런 시작
        // ─────────────────────────────────────────────────────────
        public void StartNewRun(HeroData[] startingPick)
        {
            Roster.Clear();
            InventoryWeapons.Clear();
            InventoryEquipment.Clear();
            SkillBookCounts.Clear();
            CurrentRound = 0;
            IsRunOver = false;

            // 2명으로 시작
            int n = Mathf.Min(2, startingPick.Length);
            for (int i = 0; i < n; i++) Roster.Add(new Hero(startingPick[i]));

            if (battleField != null) battleField.OnBattleEnded += OnBattleResult;

            StartNextRound();
        }

        public bool TryRecruitHero(HeroData data)
        {
            if (Roster.Count >= MaxHeroes) return false;
            Roster.Add(new Hero(data));
            return true;
        }

        // ─────────────────────────────────────────────────────────
        // 라운드 흐름
        // ─────────────────────────────────────────────────────────
        public void StartNextRound()
        {
            if (IsRunOver) return;
            CurrentRound++;
            if (CurrentRound > TotalRounds) { IsRunOver = true; OnRunCompleted?.Invoke(); return; }

            OnRoundStarted?.Invoke(CurrentRound);
            var enemies = BuildEnemiesForRound(CurrentRound);
            battleField.StartBattle(Roster, enemies);
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

            // 보상 제시
            _pendingRewards = BuildRewardOptions(CurrentRound);
            AutoSave();
            OnRewardOffered?.Invoke(_pendingRewards);
        }

        /// <summary>
        /// 플레이어가 보상 1개를 선택한 뒤 호출.
        /// targetHero: 무기/스킬일 때 적용 대상. 장비는 null이면 인벤토리로.
        /// </summary>
        public void PickReward(int index, Hero targetHero = null)
        {
            if (_pendingRewards == null) return;
            if (index < 0 || index >= _pendingRewards.Count) return;

            ApplyReward(_pendingRewards[index], targetHero);
            _pendingRewards = null;

            // 다음 라운드로
            StartNextRound();
        }

        private void ApplyReward(RewardOption r, Hero target)
        {
            switch (r.type)
            {
                case RewardType.Weapon:
                    if (target != null) target.weapon = r.weapon;
                    else                InventoryWeapons.Add(r.weapon);
                    break;

                case RewardType.Equipment:
                    if (target != null)
                    {
                        if (r.equipment.slot == EquipmentSlot.Armor)     target.armor = r.equipment;
                        else if (r.equipment.slot == EquipmentSlot.Accessory) target.accessory = r.equipment;
                        else InventoryEquipment.Add(r.equipment);
                    }
                    else InventoryEquipment.Add(r.equipment);
                    break;

                case RewardType.Skill:
                    // 스킬북 → 카운트 누적, 3개면 합성 가능 알림
                    if (!SkillBookCounts.ContainsKey(r.skill)) SkillBookCounts[r.skill] = 0;
                    SkillBookCounts[r.skill]++;
                    if (target != null && SkillBookCounts[r.skill] >= 3
                        && target.TryFuseSkill(r.skill, SkillBookCounts[r.skill]))
                    {
                        SkillBookCounts[r.skill] -= 3;
                    }
                    break;
            }
        }

        // ─────────────────────────────────────────────────────────
        // 콘텐츠 빌더 (스텁) — 데이터 테이블/난이도 곡선으로 교체 가능
        // ─────────────────────────────────────────────────────────
        protected virtual List<EnemySpawn> BuildEnemiesForRound(int round)
        {
            var list = new List<EnemySpawn>();
            int count = Mathf.Clamp(2 + round / 2, 2, 8);
            float hpScale  = 1f + (round - 1) * 0.15f;
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
                        attack      = 8  * atkScale,
                        defense     = 1,
                        maxHp       = 60 * hpScale,
                        critRate    = 0.05f,
                        critDamage  = 1.5f,
                        attackSpeed = 100,
                        range       = 0,
                        moveSpeed   = 1f
                    },
                    weapon = null,
                    skills = null
                });
            }
            return list;
        }

        protected virtual List<RewardOption> BuildRewardOptions(int round)
        {
            // 가장 단순한 규칙: 무기/장비/스킬 각 1개씩 랜덤 뽑기
            var list = new List<RewardOption>();
            if (weaponPool != null && weaponPool.Length > 0)
                list.Add(new RewardOption { type = RewardType.Weapon,
                    weapon = weaponPool[UnityEngine.Random.Range(0, weaponPool.Length)] });
            if (equipmentPool != null && equipmentPool.Length > 0)
                list.Add(new RewardOption { type = RewardType.Equipment,
                    equipment = equipmentPool[UnityEngine.Random.Range(0, equipmentPool.Length)] });
            if (skillPool != null && skillPool.Length > 0)
                list.Add(new RewardOption { type = RewardType.Skill,
                    skill = skillPool[UnityEngine.Random.Range(0, skillPool.Length)] });
            return list;
        }

        // ─────────────────────────────────────────────────────────
        // 저장 훅 — SaveSystem에 연결
        // ─────────────────────────────────────────────────────────
        private void AutoSave()
        {
            // TODO: JsonUtility / 별도 SaveSystem 호출
            // 핵심: Roster, CurrentRound, 인벤토리, SkillBookCounts 직렬화
            Debug.Log($"[AutoSave] round={CurrentRound}, heroes={Roster.Count}");
        }
    }
}
