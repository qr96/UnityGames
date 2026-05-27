using System;
using UnityEngine;

/// <summary>
/// 적이 죽었을 때 전달되는 정보. 코인 프리팹은 여기 없음 - 그건 CoinDropper의 책임.
/// </summary>
public struct EnemyDeathInfo
{
    public Vector3 position;
    public int coinDropAmount;
    public int xpReward;
}

[CreateAssetMenu(menuName = "Runner/Events/Enemy Died Channel", fileName = "EnemyDiedChannel")]
public class EnemyDiedChannel : ScriptableObject
{
    public event Action<EnemyDeathInfo> OnRaised;

    public void Raise(EnemyDeathInfo info) => OnRaised?.Invoke(info);

    void OnDisable() => OnRaised = null;
}