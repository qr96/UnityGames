using System;
using UnityEngine;

/// <summary>
/// 플레이어 골드 관리.
/// </summary>
public class PlayerGold : MonoBehaviour
{
    public static PlayerGold Instance { get; private set; }

    [SerializeField] int _startGold = 0;

    public int Gold { get; private set; }

    public event Action<int> OnGoldChanged; // 현재 골드

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Gold = _startGold;
    }

    public void Add(int amount)
    {
        if (amount <= 0) return;
        Gold += amount;
        OnGoldChanged?.Invoke(Gold);
    }

    public bool TrySpend(int amount)
    {
        if (amount <= 0 || Gold < amount)
        {
            Debug.LogWarning("[PlayerGold] 골드가 부족합니다.");
            return false;
        }

        Gold -= amount;
        OnGoldChanged?.Invoke(Gold);
        return true;
    }
}