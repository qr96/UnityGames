using GameData;
using System;
using UnityEngine;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance;

    public PlayerData Data { get; private set; }
    public UnitModel Model { get; private set; }

    public event Action<long, long> OnHpChanged;
    public event Action<long, long> OnMpChanged;
    public event Action<long, long> OnExpChanged;
    public event Action<long> OnMoneyChanged;
    public event Action<long> OnLevelChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            //Test
            Data = new PlayerData() { level = 1, pureStat = new Stat() { attack = 30, hp = 100, mp = 30 } };
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

    public void ReduceMp(long mp)
    {
        Model.ReduceMp(mp);
        OnMpChanged?.Invoke(Model.MaxStat.mp, Model.NowStat.mp);
    }

    public void GainHp(long hp)
    {
        Model.HealHp(hp);
        OnHpChanged?.Invoke(Model.MaxStat.hp, Model.NowStat.hp);
    }

    public void GainMoney(int amount, bool doCallback = true)
    {
        Data.money += amount;
        if (doCallback)
            OnMoneyChanged?.Invoke(Data.money);
    }

    public void GainExp(long amount)
    {
        var needExp = StatBalancer.GetPlayerExpRequired(Data.level);
        Data.nowExp += amount;
        
        while (Data.nowExp >= needExp)
        {
            Data.nowExp -= needExp;
            Addlevel();
            needExp = StatBalancer.GetPlayerExpRequired(Data.level);
        }

        OnExpChanged?.Invoke(needExp, Data.nowExp);
    }

    void Addlevel()
    {
        Data.level++;
        Model.SetStat(StatBalancer.GetPlayerStatsByLevel(Data.level, Model.MaxStat));
        OnLevelChanged?.Invoke(Data.level);
    }

    // 초기 로드 시 UI를 한 번 초기화해주는 함수 (Scene 로드 직후 호출)
    public void InitializeUI()
    {
        OnHpChanged?.Invoke(Model.MaxStat.hp, Model.NowStat.hp);
        OnMpChanged?.Invoke(Model.MaxStat.mp, Model.NowStat.mp);
        OnMoneyChanged?.Invoke(Data.money);
        OnExpChanged?.Invoke(StatBalancer.GetPlayerExpRequired(Data.level), Data.nowExp);
        OnLevelChanged?.Invoke(Data.level);
    }
}
