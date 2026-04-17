using UnityEngine;

[RequireComponent(typeof(EnemyStats))]
public class EnemyDropper : MonoBehaviour
{
    [Header("드롭 설정")]
    public int dropCount = 5;
    public string coinPath = "Prefabs/DroppedItems/Coin";

    EnemyStats _stats;

    public void Init(EnemyStats stats)
    {
        _stats = stats;
        _stats.OnDied += GrantXP;
        _stats.OnDied += SpawnItems;
    }

    void GrantXP()
    {
        if (PlayerStats.Instance == null) return;

        int xp = GameFormulas.GetXPReward(_stats.Data, PlayerStats.Instance.Level);
        PlayerStats.Instance.AddXP(xp);

        Debug.Log($"[EnemyDropper] XP +{xp} ({_stats.Data.enemyType} Lv.{_stats.Data.level})");
    }

    void SpawnItems()
    {
        for (int i = 0; i < dropCount; i++) SpawnCoin();
    }

    void SpawnCoin()
    {
        if (!PoolManager.Instance.TryCreate(coinPath, out var prefab)) return;
        var coin = prefab.GetComponent<DroppedItem>();
        if (coin == null) return;

        var dir = new Vector3(
            Random.Range(-1f, 1f), 1f, Random.Range(-1f, 1f)
        ).normalized * 3f;

        // itemCode에 코인당 골드 수량 전달
        int goldPerCoin = _stats.Data.goldDrop / Mathf.Max(1, dropCount);

        coin.rigid.position = transform.position;
        coin.SpawnItem(0, goldPerCoin, dir, OnCoinCollected);
    }

    void OnCoinCollected(int itemId, int itemCode, DroppedItem item)
    {
        PlayerGold.Instance?.Add(itemCode); // itemCode = 골드 수량
    }
}
