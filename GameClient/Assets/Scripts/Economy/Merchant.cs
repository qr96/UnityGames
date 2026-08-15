using System.Text;
using UnityEngine;

// 행상인. config 주기로 등장→머무름→퇴장 반복.
// 방문 중 E: 설정된 매입 대상(장작·식량) 보유분을 전부 팔아 골드 획득.
// 얼마를 남기고 팔지는 플레이어가 '언제 파는가'로 판단(잉여 판매). 수량 선택 UI는 이후 과제.
public class Merchant : InteractableBase
{
    [SerializeField] private MerchantConfig config;
    [SerializeField] private Inventory inventory;      // 비우면 씬에서 찾음
    [Tooltip("방문 중에만 켜질 비주얼. 스크립트가 붙은 루트가 아니라 자식 오브젝트로")]
    [SerializeField] private GameObject visualRoot;

    private bool visiting;
    private float timer;

    public bool IsVisiting => visiting;

    private void Start()
    {
        if (inventory == null) inventory = FindObjectOfType<Inventory>();
        SetVisiting(false);
        timer = config != null ? config.visitIntervalSeconds : 60f;
    }

    private void Update()
    {
        if (config == null) return;
        timer -= GameClock.Delta;
        if (timer > 0f) return;

        if (visiting) { SetVisiting(false); timer = config.visitIntervalSeconds; }
        else { SetVisiting(true); timer = config.staySeconds; }
    }

    private void SetVisiting(bool v)
    {
        visiting = v;
        if (visualRoot != null) visualRoot.SetActive(v);
    }

    public override string Prompt
    {
        get
        {
            if (!visiting || config == null || inventory == null) return "행상인";

            var sb = new StringBuilder("판매:");
            int gold = 0;
            bool any = false;
            if (config.buyPrices != null)
            {
                for (int i = 0; i < config.buyPrices.Length; i++)
                {
                    MerchantConfig.BuyPrice bp = config.buyPrices[i];
                    int qty = inventory.Get(bp.item);
                    if (qty <= 0) continue;
                    any = true;
                    gold += qty * bp.goldPerUnit;
                    sb.Append($" {PlayerCrafting.ItemLabel(bp.item)}x{qty}");
                }
            }
            return any ? sb.Append($" → {gold}G").ToString() : "행상인 (팔 것 없음)";
        }
    }

    // 방문 중이면 타겟은 되게(프롬프트로 상황 표시). 실제 판매는 Interact에서 판정.
    public override bool CanInteract(GameObject interactor)
        => visiting && config != null && inventory != null;

    public override void Interact(GameObject interactor)
    {
        if (!visiting || config == null || inventory == null || config.buyPrices == null) return;

        int gold = 0;
        for (int i = 0; i < config.buyPrices.Length; i++)
        {
            MerchantConfig.BuyPrice bp = config.buyPrices[i];
            int qty = inventory.Get(bp.item);
            if (qty <= 0) continue;
            inventory.TrySpend(bp.item, qty);
            gold += qty * bp.goldPerUnit;
        }
        if (gold > 0) inventory.AddGold(gold);
        else Debug.Log("[행상인] 팔 물건 없음");
    }

}