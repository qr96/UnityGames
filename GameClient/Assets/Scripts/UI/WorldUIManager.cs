using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 데미지 채널을 구독해 데미지 팝업과 적 체력바를 풀에서 꺼내 표시.
/// 체력바는 적당 하나만 유지 (이미 있으면 갱신).
/// 모든 월드 UI는 이 매니저의 Canvas(Screen Space) 아래에 들어감.
/// </summary>
public class WorldUIManager : MonoBehaviour
{
    public static WorldUIManager Instance { get; private set; }

    [Header("Channel")]
    public DamageDealtChannel damageChannel;

    [Header("Pool Paths (Resources 경로)")]
    public string popupPath = "UI/DamagePopup";
    public string healthBarPath = "UI/EnemyHealthBar";

    [Header("Offsets")]
    [Tooltip("데미지 팝업이 뜰 높이 (적 중심에서 위로). 적 머리 위에 뜨도록 조정.")]
    public float popupHeightOffset = 1.5f;

    [Header("Canvas")]
    [Tooltip("월드 UI가 들어갈 Screen Space Canvas의 Transform")]
    public Transform uiParent;

    // 적별 활성 체력바 추적 (적당 하나)
    private readonly Dictionary<Enemy, EnemyHealthBarUI> activeBars = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void OnEnable()
    {
        if (damageChannel != null) damageChannel.OnRaised += HandleDamage;
    }

    void OnDisable()
    {
        if (damageChannel != null) damageChannel.OnRaised -= HandleDamage;
    }

    void HandleDamage(DamageInfo info)
    {
        SpawnPopup(info);
        UpdateHealthBar(info);
    }

    void SpawnPopup(DamageInfo info)
    {
        if (PoolManager.Instance == null) return;
        if (!PoolManager.Instance.TryCreate(popupPath, out GameObject obj)) return;

        PlaceUnderCanvas(obj);

        var popup = obj.GetComponent<DamagePopupUI>();
        if (popup != null)
        {
            Vector3 popupPos = info.position + Vector3.up * popupHeightOffset;
            popup.Play(info.amount, popupPos, info.isCritical);
        }
    }

    void UpdateHealthBar(DamageInfo info)
    {
        if (info.source == null) return; // 적이 아닌 데미지면 체력바 없음

        // 이미 이 적의 체력바가 있으면 갱신
        if (activeBars.TryGetValue(info.source, out var existing) && existing != null
            && existing.TrackedEnemy == info.source)
        {
            existing.Refresh(info.hpRatio);
            return;
        }

        if (PoolManager.Instance == null) return;
        if (!PoolManager.Instance.TryCreate(healthBarPath, out GameObject obj)) return;

        PlaceUnderCanvas(obj);

        var bar = obj.GetComponent<EnemyHealthBarUI>();
        if (bar != null)
        {
            bar.Bind(info.source, info.hpRatio);
            activeBars[info.source] = bar;
        }

        CleanupDeadEntries();
    }

    // 죽거나 사라진 적의 딕셔너리 항목 정리 (메모리 누수 방지)
    private readonly List<Enemy> toRemove = new();
    void CleanupDeadEntries()
    {
        toRemove.Clear();
        foreach (var kvp in activeBars)
        {
            if (kvp.Key == null || kvp.Key.IsDead || kvp.Value == null)
                toRemove.Add(kvp.Key);
        }
        foreach (var key in toRemove)
            activeBars.Remove(key);
    }

    void PlaceUnderCanvas(GameObject obj)
    {
        if (uiParent != null)
            obj.transform.SetParent(uiParent, false);
    }
}