using System;
using UnityEngine;

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