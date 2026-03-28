using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHp = 30;
    int _hp;

    void Awake() => _hp = maxHp;

    public void TakeDamage(int dmg)
    {
        _hp -= dmg;
        if (_hp <= 0) Die();
    }

    void Die() => Destroy(gameObject);
}
