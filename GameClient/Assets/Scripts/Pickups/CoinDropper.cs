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

            // 풀에서 꺼냄 (없으면 fallback으로 직접 생성)
            GameObject coin = PoolManager.Instance != null
                ? PoolManager.Instance.Get(coinPrefab)
                : Instantiate(coinPrefab);
            if (coin == null) continue;

            // Get 시 Coin.OnEnable이 상태 리셋. 위치는 rb까지 즉시 동기화(보간 잔상 방지).
            Vector3 spawnPos = info.position + offset;
            if (coin.TryGetComponent(out Coin coinComp))
                coinComp.Spawn(spawnPos);
            else
                coin.transform.position = spawnPos;
        }
    }
}
