using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AutoBattler.Data;
using AutoBattler.Rounds;

namespace AutoBattler.UI
{
    /// <summary>
    /// 전투 HUD. RunManager 이벤트와 화면들을 연결.
    ///
    /// 화면 흐름:
    ///   StartPanel       → "전투 시작" → 라운드 1
    ///   HudPanel         (전투 중 라운드 표시)
    ///   RewardPanel      라운드 클리어 → 받은 스킬 목록 + "확인"
    ///                    → ConfirmRewards() → LoadoutScreen 으로
    ///   LoadoutScreen    장착/합성 → "다음 전투" → ProceedToNextRound()
    ///   ResultPanel      런 완료/실패
    /// </summary>
    public class BattleHUD : MonoBehaviour
    {
        [Header("매니저")]
        public RunManager runManager;

        [Header("시작 화면")]
        public GameObject startPanel;
        public Button startButton;
        public HeroData[] startingHeroes; // 인스펙터에서 2명 드래그

        [Header("HUD")]
        public GameObject hudPanel;
        public TMP_Text roundText;

        [Header("보상 패널")]
        public GameObject rewardPanel;
        public TMP_Text rewardListText; // 받은 스킬 나열
        public Button rewardConfirmButton;

        [Header("장착/합성 화면")]
        public LoadoutScreen loadoutScreen; // GameObject가 활성/비활성으로 토글됨

        [Header("결과 패널")]
        public GameObject resultPanel;
        public TMP_Text resultText;
        public Button restartButton;

        // ─────────────────────────────────────────────────────────
        private void Awake()
        {
            if (startButton != null) startButton.onClick.AddListener(OnStartClicked);
            if (restartButton != null) restartButton.onClick.AddListener(OnStartClicked);
            if (rewardConfirmButton != null) rewardConfirmButton.onClick.AddListener(OnRewardConfirmClicked);
        }

        private void OnEnable()
        {
            if (runManager == null) return;
            runManager.OnRoundStarted += HandleRoundStarted;
            runManager.OnRewardOffered += HandleRewardOffered;
            runManager.OnLoadoutReady += HandleLoadoutReady;
            runManager.OnRunCompleted += HandleRunCompleted;
            runManager.OnRunFailed += HandleRunFailed;
        }

        private void OnDisable()
        {
            if (runManager == null) return;
            runManager.OnRoundStarted -= HandleRoundStarted;
            runManager.OnRewardOffered -= HandleRewardOffered;
            runManager.OnLoadoutReady -= HandleLoadoutReady;
            runManager.OnRunCompleted -= HandleRunCompleted;
            runManager.OnRunFailed -= HandleRunFailed;
        }

        private void Start()
        {
            ShowStartScreen();
        }

        // ─────────────────────────────────────────────────────────
        // 화면 토글
        // ─────────────────────────────────────────────────────────
        private void ShowStartScreen()
        {
            SetActive(startPanel, true);
            SetActive(hudPanel, false);
            SetActive(rewardPanel, false);
            SetActive(loadoutScreen?.gameObject, false);
            SetActive(resultPanel, false);
        }

        private void ShowHud()
        {
            SetActive(startPanel, false);
            SetActive(hudPanel, true);
            SetActive(rewardPanel, false);
            SetActive(loadoutScreen?.gameObject, false);
            SetActive(resultPanel, false);
        }

        private void ShowReward(List<SkillData> skills)
        {
            SetActive(rewardPanel, true);

            if (rewardListText != null)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Rewards:");
                foreach (var s in skills)
                    sb.AppendLine($"  · {s.displayName}");
                rewardListText.text = sb.ToString();
            }
        }

        private void ShowLoadout()
        {
            SetActive(rewardPanel, false);
            SetActive(loadoutScreen?.gameObject, true);
            loadoutScreen?.Rebuild();
        }

        private void ShowResult(string message)
        {
            SetActive(rewardPanel, false);
            SetActive(loadoutScreen?.gameObject, false);
            SetActive(resultPanel, true);
            if (resultText != null) resultText.text = message;
        }

        // ─────────────────────────────────────────────────────────
        // 버튼 핸들러
        // ─────────────────────────────────────────────────────────
        private void OnStartClicked()
        {
            if (runManager == null) { UnityEngine.Debug.LogError("[BattleHUD] runManager 미연결"); return; }
            if (startingHeroes == null || startingHeroes.Length < 2)
            {
                UnityEngine.Debug.LogError("[BattleHUD] startingHeroes 2명 필요");
                return;
            }
            ShowHud();
            runManager.StartNewRun(startingHeroes);
        }

        private void OnRewardConfirmClicked()
        {
            runManager.ConfirmRewards();   // → OnLoadoutReady → HandleLoadoutReady
        }

        // ─────────────────────────────────────────────────────────
        // RunManager 이벤트
        // ─────────────────────────────────────────────────────────
        private void HandleRoundStarted(int round)
        {
            if (roundText != null)
                roundText.text = $"Round {round} / {RunManager.TotalRounds}";
            ShowHud();
        }

        private void HandleRewardOffered(List<SkillData> skills)
        {
            ShowReward(skills);
        }

        private void HandleLoadoutReady()
        {
            ShowLoadout();
        }

        private void HandleRunCompleted()
        {
            ShowResult("승리!\n모든 라운드 클리어");
        }

        private void HandleRunFailed()
        {
            ShowResult("패배...\n다시 시도하세요");
        }

        private static void SetActive(GameObject go, bool on)
        {
            if (go != null && go.activeSelf != on) go.SetActive(on);
        }
    }
}