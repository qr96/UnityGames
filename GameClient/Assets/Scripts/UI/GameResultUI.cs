using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 상태에 반응해 게임오버/클리어 패널을 표시.
/// GameManager의 상태 변화를 구독.
/// </summary>
public class GameResultUI : MonoBehaviour
{
    [Header("Panels")]
    [Tooltip("게임오버 패널 (비활성으로 시작)")]
    public GameObject gameOverPanel;

    [Tooltip("스테이지 클리어 패널 (비활성으로 시작)")]
    public GameObject stageClearPanel;

    [Header("Buttons")]
    public Button restartButton;
    public Button clearRestartButton;

    private bool subscribed = false;

    void Awake()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (stageClearPanel != null) stageClearPanel.SetActive(false);

        if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
        if (clearRestartButton != null) clearRestartButton.onClick.AddListener(OnRestartClicked);
    }

    void OnEnable() => TrySubscribe();
    void Start() => TrySubscribe();

    void OnDisable()
    {
        if (subscribed && GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
            subscribed = false;
        }
    }

    void TrySubscribe()
    {
        if (subscribed || GameManager.Instance == null) return;
        GameManager.Instance.OnStateChanged += HandleStateChanged;
        subscribed = true;
    }

    void HandleStateChanged(GameState prev, GameState next)
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(next == GameState.GameOver);
        if (stageClearPanel != null) stageClearPanel.SetActive(next == GameState.StageClear);
    }

    void OnRestartClicked()
    {
        if (GameManager.Instance != null) GameManager.Instance.RestartStage();
    }
}
