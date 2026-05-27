using UnityEngine;

/// <summary>
/// 코인 프리팹을 들고 있는 유일한 곳.
/// 적 사망 채널을 듣고 자기가 보유한 프리팹으로 코인 스폰.
/// </summary>
public class CoinDropper : MonoBehaviour
{
    [Header("Channel")]
    public EnemyDiedChannel diedChannel;

    [Header("Coin")]
    [Tooltip("떨굴 코인 프리팹. 여기 한 곳에만 참조 둠.")]
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