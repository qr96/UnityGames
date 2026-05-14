using UnityEngine;
using AutoBattler.Data;
using AutoBattler.Rounds;

namespace AutoBattler.Debugging
{
    /// <summary>
    /// 디버그용: Play 누르자마자 RunManager.StartNewRun 호출.
    /// 정식 흐름에서는 MainMenu UI가 이 일을 함.
    /// </summary>
    public class DebugAutoStart : MonoBehaviour
    {
        [Tooltip("RunManager 드래그")]
        public RunManager runManager;

        [Tooltip("시작할 영웅 2명")]
        public HeroData[] startingHeroes;

        [Tooltip("씬 시작 후 몇 초 뒤에 시작할지 (0=즉시)")]
        public float startDelay = 0.2f;

        private void Start()
        {
            if (runManager == null)
            {
                UnityEngine.Debug.LogError("[DebugAutoStart] RunManager가 연결되지 않았습니다.");
                return;
            }
            if (startingHeroes == null || startingHeroes.Length < 2)
            {
                UnityEngine.Debug.LogError("[DebugAutoStart] startingHeroes에 영웅 2명 이상 필요.");
                return;
            }
            Invoke(nameof(Begin), startDelay);
        }

        private void Begin()
        {
            UnityEngine.Debug.Log("[DebugAutoStart] StartNewRun 호출");
            runManager.StartNewRun(startingHeroes);
        }
    }
}