using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float speed = 8f;
    public float knockbackForce = 5f;
    public float knockbackDecay = 5f;
    public int maxHP = 2;

    private float knockbackVelocity = 0f;
    private int currentHP;

    void Start()
    {
        currentHP = maxHP;
    }

    void Update()
    {
        float moveZ = -speed * Time.deltaTime;
        moveZ += knockbackVelocity * Time.deltaTime;
        transform.Translate(0, 0, moveZ, Space.World);

        knockbackVelocity = Mathf.MoveTowards(knockbackVelocity, 0f, knockbackDecay * Time.deltaTime);

        if (transform.position.z < -15f) Destroy(gameObject);
    }

    public void TakeHit(int damage)
    {
        currentHP -= damage;
        knockbackVelocity = knockbackForce;

        if (currentHP <= 0)
        {
            Destroy(gameObject);
        }
    }
}