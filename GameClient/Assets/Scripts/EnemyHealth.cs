using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHp = 30;
    int _hp;
    EnemyAI ai;

    void Awake()
    {
        _hp = maxHp;
        ai = GetComponent<EnemyAI>();
    }

    public void TakeDamage(int dmg)
    {
        _hp -= dmg;
        if (_hp <= 0) Die();
    }

    void Die()
    {
        //Destroy(gameObject);
    }
}
