using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Playing,     // 일반 플레이 (잡몹 웨이브)
    LevelUp,     // 레벨업 선택 중 (일시정지)
    BossFight,   // 보스전
    GameOver,    // 사망
    StageClear,  // 스테이지 클리어
}

/// <summary>
/// 게임 상태 중앙 관리. Time.timeScale 제어와 상태 전환을 한 곳에서.
/// 각 시스템은 이벤트(죽음/레벨업/클리어)를 GameManager에 알리고,
/// GameManager가 상태를 바꾸며 필요한 곳에 통지.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState State { get; private set; } = GameState.Playing;

    /// <summary>상태가 바뀔 때 발행. (이전, 새 상태)</summary>
    public event Action<GameState, GameState> OnStateChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // 씬 시작 시 항상 정상 속도 (이전 판에서 0으로 멈춘 채 씬 재로드 대비)
        Time.timeScale = 1f;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        SetState(GameState.Playing);
    }

    public void SetState(GameState newState)
    {
        if (State == newState) return;

        GameState prev = State;
        State = newState;

        // 상태별 시간 제어
        switch (newState)
        {
            case GameState.Playing:
            case GameState.BossFight:
                Time.timeScale = 1f;
                break;

            case GameState.LevelUp:
            case GameState.GameOver:
            case GameState.StageClear:
                Time.timeScale = 0f;
                break;
        }

        OnStateChanged?.Invoke(prev, newState);
        Debug.Log($"[GameManager] {prev} → {newState}");
    }

    // ─── 각 시스템이 호출하는 통지 메서드 ───

    /// <summary>플레이어 사망.</summary>
    public void NotifyPlayerDied()
    {
        if (State == GameState.GameOver) return;
        SetState(GameState.GameOver);
    }

    /// <summary>레벨업 선택 시작.</summary>
    public void NotifyLevelUpStarted()
    {
        // 보스전 중 레벨업도 가능하므로 이전 상태 기억은 단순화: LevelUp으로
        SetState(GameState.LevelUp);
    }

    /// <summary>레벨업 선택 완료 → 플레이로 복귀.</summary>
    public void NotifyLevelUpFinished()
    {
        // 보스전 중이었으면 보스전 유지, 아니면 일반 플레이.
        // (BossFight 진입 후엔 웨이브가 다 끝난 상태이므로 Playing으로 돌아가지 않음)
        SetState(bossFightActive ? GameState.BossFight : GameState.Playing);
    }

    // 보스전 진입 여부 (보스 처치 또는 게임오버 전까지 유지)
    private bool bossFightActive = false;

    /// <summary>모든 잡몹 웨이브 클리어 → 보스전 시작.</summary>
    public void NotifyAllWavesCleared()
    {
        bossFightActive = true;
        SetState(GameState.BossFight);
    }

    /// <summary>보스 처치 → 스테이지 클리어.</summary>
    public void NotifyBossDefeated()
    {
        bossFightActive = false;
        SetState(GameState.StageClear);
    }

    // ─── 게임 흐름 제어 ───

    public void RestartStage()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
