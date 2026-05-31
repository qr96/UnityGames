using UnityEngine;

public class CoinDropper : MonoBehaviour
{
    [Header("Channel")]
    public EnemyDiedChannel diedChannel;

    [Header("Coin")]
    public GameObject coinPrefab;

    [Header("Drop Pattern")]
    public float spreadRadius = 0.5f;
    public float dropHeight = 0.5f;

    void OnEnable()
    {
        if (diedChannel != null) diedChannel.OnRaised += HandleEnemyDied;
    }

    void OnDisable()
    {
        if (diedChannel != null) diedChannel.OnRaised -= HandleEnemyDied;
    }

    void HandleEnemyDied(EnemyDeathInfo info)
    {
        if (coinPrefab == null || info.coinDropAmount <= 0) return;

        for (int i = 0; i < info.coinDropAmount; i++)
        {
            Vector3 offset = new Vector3(
                Random.Range(-spreadRadius, spreadRadius),
                dropHeight,
                Random.Range(-spreadRadius, spreadRadius)
            );
            Instantiate(coinPrefab, info.position + offset, Quaternion.identity);
        }
    }
}