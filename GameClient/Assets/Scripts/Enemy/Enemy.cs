using UnityEngine;

/// <summary>
/// 적 컴포넌트 초기화 코디네이터.
/// 스폰 시점에 Init(data) 하나만 호출하면 모든 컴포넌트 초기화.
/// 
/// 사용 예:
///   PoolManager.Instance.TryCreate("Enemy/Goblin", out var obj);
///   obj.GetComponent<Enemy>().Init(goblinData);
/// </summary>
public class Enemy : MonoBehaviour
{
    EnemyStats _stats;
    EnemyAI _ai;
    EnemyDropper _dropper;
    EnemyKnockback _knockback;

    void Awake()
    {
        _stats = GetComponent<EnemyStats>();
        _ai = GetComponent<EnemyAI>();
        _dropper = GetComponent<EnemyDropper>();
        _knockback = GetComponent<EnemyKnockback>();
    }

    public void Init(EnemyData data)
    {
        _stats.Init(data);
        _ai?.Init(_stats);
        _dropper?.Init(_stats);
        _knockback?.Init(_stats);
    }
}