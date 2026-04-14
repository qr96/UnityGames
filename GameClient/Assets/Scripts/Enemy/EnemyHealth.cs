using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHp = 30;
    int _hp;

    public event Action<Vector3> OnDamaged;  // 피격 방향
    public event Action OnDied;

    void Awake() => _hp = maxHp;

    public void TakeDamage(int dmg, Vector3 hitDir = default)
    {
        if (_hp <= 0) return;
        _hp -= dmg;
        OnDamaged?.Invoke(hitDir);
        if (_hp <= 0) OnDied?.Invoke();
    }
}
