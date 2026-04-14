using System;
using UnityEngine;

/// <summary>
/// 적 런타임 상태 (현재 HP만 관리).
/// 정적 데이터(스탯, 레벨, XP 등)는 EnemyData(ScriptableObject)에서 읽어옴.
/// 같은 종류의 적은 동일한 EnemyData 에셋을 공유.
/// </summary>
public class EnemyStats : MonoBehaviour, IDamageable
{
    [SerializeField] EnemyData _data;
    public EnemyData Data => _data;

    int _currentHP;
    public int CurrentHP => _currentHP;

    public event Action<Vector3> OnDamaged;
    public event Action OnDied;

    void Awake()
    {
        if (_data == null)
        {
            Debug.LogError($"[EnemyStats] {gameObject.name} 에 EnemyData가 할당되지 않았습니다.");
            return;
        }
        _currentHP = _data.maxHp;
    }


    public void TakeDamage(int damage, Vector3 hitDir = default)
    {
        if (_currentHP <= 0) return;

        int actual = GameFormulas.GetActualDamage(damage, _data.defense);
        _currentHP = Mathf.Max(0, _currentHP - actual);
        OnDamaged?.Invoke(hitDir);

        if (_currentHP <= 0) OnDied?.Invoke();
    }
}
