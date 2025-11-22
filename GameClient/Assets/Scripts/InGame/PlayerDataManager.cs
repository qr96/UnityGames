using GameData;
using System;
using UnityEngine;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance;

    public PlayerData Data { get; private set; }
    public UnitModel Model { get; private set; }

    public event Action<long, long> OnHpChanged;
    public event Action<long, long> OnExpChanged;
    public event Action<long> OnMoneyChanged;
    public event Action<long> OnLevelChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            //Test
            Data = new PlayerData() { level = 1, pureStat = new Stat() { attack = 5, hp = 100, mp = 30 } };
            Model = new UnitModel("Player", Data.pureStat);
            Model.Reset();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void TakeDamage(long damage)
    {
        Model.TakeDamage(damage);
        OnHpChanged?.Invoke(Model.MaxStat.hp, Model.NowStat.hp);
    }

    public void GainMoney(int amount)
    {
        Data.money += amount;
        OnMoneyChanged?.Invoke(Data.money);
    }

    public void GainExp(long amount)
    {
        var needExp = GetNeedExp(Data.level);
        Data.nowExp += amount;
        OnExpChanged?.Invoke(needExp, Data.nowExp);

        while (Data.nowExp >= needExp)
        {
            Data.nowExp -= needExp;
            Addlevel();
            needExp = GetNeedExp(Data.level);
        }
    }

    void Addlevel()
    {
        Data.level++;
        OnLevelChanged?.Invoke(Data.level);
    }

    long GetNeedExp(int level)
    {
        return level * 100;
    }

    // 초기 로드 시 UI를 한 번 초기화해주는 함수 (Scene 로드 직후 호출)
    public void InitializeUI()
    {
        OnHpChanged?.Invoke(Model.MaxStat.hp, Model.NowStat.hp);
        OnMoneyChanged?.Invoke(Data.money);
        OnExpChanged?.Invoke(GetNeedExp(Data.level), Data.nowExp);
        OnLevelChanged?.Invoke(Data.level);
    }
}
