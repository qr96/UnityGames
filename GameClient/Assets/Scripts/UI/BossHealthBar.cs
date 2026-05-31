using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 보스 체력바 (화면 상단 고정). 보스 등장 시 나타나고 처치 시 사라짐.
/// Boss의 이벤트를 구독.
/// </summary>
public class BossHealthBar : MonoBehaviour
{
    [Header("References")]
    public GameObject barRoot;     // 켜고 끌 루트 (이 컴포넌트와 별개 오브젝트)
    public Slider slider;
    public TMP_Text nameText;

    [Header("Boss Name")]
    public string bossName = "BOSS";

    private Boss boss;

    void Awake()
    {
        if (barRoot != null && barRoot != gameObject) barRoot.SetActive(false);
    }

    void OnEnable() => TryBind();
    void Start() => TryBind();

    void OnDisable()
    {
        Unbind();
    }

    void TryBind()
    {
        if (boss != null) return;
        if (Boss.ActiveBoss == null) return;

        boss = Boss.ActiveBoss;
        boss.OnAppeared += HandleAppeared;
        boss.OnHPChanged += HandleHPChanged;
        boss.OnDefeated += HandleDefeated;

        // 이미 등장한 상태면 즉시 표시
        HandleAppeared(boss);
        HandleHPChanged(boss.CurrentHP, boss.MaxHP);
    }

    void Unbind()
    {
        if (boss == null) return;
        boss.OnAppeared -= HandleAppeared;
        boss.OnHPChanged -= HandleHPChanged;
        boss.OnDefeated -= HandleDefeated;
        boss = null;
    }

    void Update()
    {
        // 보스가 아직 안 떴으면 계속 바인드 시도 (보스는 웨이브 후 늦게 스폰됨)
        if (boss == null) TryBind();
    }

    void HandleAppeared(Boss b)
    {
        if (barRoot != null && barRoot != gameObject) barRoot.SetActive(true);
        if (nameText != null) nameText.text = bossName;
    }

    void HandleHPChanged(int current, int max)
    {
        if (slider != null) slider.value = max > 0 ? (float)current / max : 0f;
    }

    void HandleDefeated()
    {
        if (barRoot != null && barRoot != gameObject) barRoot.SetActive(false);
    }
}
