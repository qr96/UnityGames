using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 적 체력바. 풀에서 꺼내져 특정 적을 추적.
/// Slider로 HP 비율 표시. 일정 시간 갱신 없으면 풀 반납.
/// </summary>
[RequireComponent(typeof(Poolable))]
public class EnemyHealthBarUI : WorldFollowUI
{
    [Header("References")]
    public Slider slider;

    [Header("Behavior")]
    [Tooltip("마지막 데미지 후 이 시간 지나면 숨김(반납)(초)")]
    public float hideDelay = 1.5f;

    [Tooltip("적 머리 위 높이 오프셋")]
    public float heightOffset = 2f;

    private Poolable poolable;
    private float hideTimer;
    private Enemy trackedEnemy;

    public Enemy TrackedEnemy => trackedEnemy;

    protected override void Awake()
    {
        base.Awake();
        poolable = GetComponent<Poolable>();
    }

    /// <summary>풀에서 꺼낸 직후. 추적할 적과 초기 비율 설정.</summary>
    public void Bind(Enemy enemy, float ratio)
    {
        trackedEnemy = enemy;
        SetTarget(enemy.transform, Vector3.up * heightOffset);
        if (slider != null) slider.value = ratio;
        hideTimer = hideDelay;
    }

    /// <summary>이미 추적 중인 적이 또 맞았을 때 갱신.</summary>
    public void Refresh(float ratio)
    {
        if (slider != null) slider.value = ratio;
        hideTimer = hideDelay;
    }

    protected override void LateUpdate()
    {
        // 추적 대상이 죽거나 사라지면 즉시 반납
        if (trackedEnemy == null || trackedEnemy.IsDead)
        {
            Release();
            return;
        }

        base.LateUpdate(); // 위치 추적

        hideTimer -= Time.deltaTime;
        if (hideTimer <= 0f) Release();
    }

    void Release()
    {
        trackedEnemy = null;
        poolable.ReleaseSelf();
    }
}
