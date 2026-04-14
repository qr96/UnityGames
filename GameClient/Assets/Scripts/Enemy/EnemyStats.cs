using System;
using UnityEngine;

/// <summary>
/// 적 런타임 상태 (현재 HP만 관리).
/// 정적 데이터는 스폰 시점에 Init()으로 주입받음.
/// </summary>
[RequireComponent(typeof(Poolable))]
public class EnemyStats : MonoBehaviour, IDamageable
{
    public EnemyData Data { get; private set; }

    int _currentHP;
    public int CurrentHP => _currentHP;

    public event Action<Vector3> OnDamaged;
    public event Action OnDied;

    /// <summary>
    /// 스폰 시점에 호출. EnemyData를 주입하고 상태를 초기화.
    /// 풀에서 재사용될 때도 반드시 호출해야 함.
    /// </summary>
    public void Init(EnemyData data)
    {
        Data = data;
        _currentHP = data.maxHp;

        // 풀에서 재사용 시 이전 구독 초기화
        OnDamaged = null;
        OnDied = null;
    }

    // ── IDamageable ───────────────────────────────────────────────────────

    public void TakeDamage(int damage, Vector3 hitDir = default)
    {
        if (_currentHP <= 0) return;

        int actual = GameFormulas.GetActualDamage(damage, Data.defense);
        _currentHP = Mathf.Max(0, _currentHP - actual);
        OnDamaged?.Invoke(hitDir);

        if (_currentHP <= 0)
        {
            OnDied?.Invoke();
            GetComponent<Poolable>().ReleaseSelf();
        }
    }
}