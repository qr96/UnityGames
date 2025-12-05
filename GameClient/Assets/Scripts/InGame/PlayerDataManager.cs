using GameData;
using System;
using UnityEngine;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance;

    public PlayerData Data { get; private set; }
    public PlayerModel Model { get; private set; }

    public event Action<long, long> OnHpChanged;
    public event Action<long, long> OnMpChanged;
    public event Action<long, long> OnExpChanged;
    public event Action<long> OnMoneyChanged;
    public event Action<long, long> OnLevelChanged; // now, prev

    public event Action<int> OnWeaponSlotChange;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            //Test
            Data = new PlayerData();
            Respawn();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Respawn()
    {
        Model = new PlayerModel("Player", new Stat() { attack = 50, hp = 100, defense = 10 });
        Model.MaxMp = 50;
        Model.Respawn();
    }

    public void TakeDamage(long damage)
    {
        Model.TakeDamage(damage);
        OnHpChanged?.Invoke(Model.MaxStat.hp, Model.NowStat.hp);
    }

    public void ReduceMp(long mp)
    {
        Model.ReduceMp(mp);
        OnMpChanged?.Invoke(Model.MaxMp, Model.NowMp);
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
        var needExp = StatBalancer.GetPlayerExpRequired(Model.Level);
        Model.Exp += amount;

        while (Model.Exp >= needExp)
        {
            Model.Exp -= needExp;
            Addlevel();
            needExp = StatBalancer.GetPlayerExpRequired(Model.Level);
        }

        OnExpChanged?.Invoke(needExp, Model.Exp);
    }

    public void AddWeaponSlotLevel()
    {
        var price = PriceBalancer.GetInhancePrice(Data.weaponSlot);
        if (Data.money > price)
        {
            Data.money -= price;
            Data.weaponSlot++;
            OnWeaponSlotChange?.Invoke(Data.weaponSlot);
        }
    }

    void Addlevel()
    {
        Model.Level++;
        Model.SetStat(StatBalancer.GetPlayerStatsByLevel(Model.Level, Model.MaxStat));
        OnLevelChanged?.Invoke(Model.Level, Model.Level - 1);
    }

    // 초기 로드 시 UI를 한 번 초기화해주는 함수 (Scene 로드 직후 호출)
    public void InitializeUI()
    {
        OnHpChanged?.Invoke(Model.MaxStat.hp, Model.NowStat.hp);
        OnMpChanged?.Invoke(Model.MaxMp, Model.NowMp);
        OnMoneyChanged?.Invoke(Data.money);
        OnExpChanged?.Invoke(StatBalancer.GetPlayerExpRequired(Model.Level), Model.Exp);
        OnLevelChanged?.Invoke(Model.Level, Model.Level);
    }
}
