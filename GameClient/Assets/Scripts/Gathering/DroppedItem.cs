using UnityEngine;

// 바닥 아이템. 두 용도 겸용:
//  - 월드 배치(나뭇가지·돌 등): Respawn Seconds > 0 → 주우면 사라졌다가 재생
//  - 노드 산출(벌목 장작 등): Respawn Seconds <= 0 → 다 주우면 제거
// 인벤토리 여유가 부족하면 들어간 만큼만 줄고 나머지는 바닥에 남는다.
public class DroppedItem : InteractableBase
{
    [SerializeField] private ResourceKind kind = ResourceKind.Firewood;
    [SerializeField] private int amount = 1;

    [Header("재생 (월드 배치용)")]
    [Tooltip("0 이하면 다 주웠을 때 제거. 양수면 그 시간(초) 뒤 원래 수량으로 재생")]
    [SerializeField] private float respawnSeconds = 0f;

    [Header("비주얼 (선택) — 비우면 렌더러를 껐다 켬")]
    [SerializeField] private GameObject visual;

    [SerializeField] private Inventory inventory; // 비우면 씬에서 찾음

    private int initialAmount;
    private bool taken;
    private float readyTime;

    public ResourceKind Kind => kind;
    public int Amount => amount;

    // 노드가 스폰 직후 호출
    public void Setup(ResourceKind newKind, int newAmount)
    {
        kind = newKind;
        amount = Mathf.Max(1, newAmount);
        initialAmount = amount;
    }

    private void Start()
    {
        if (inventory == null) inventory = FindObjectOfType<Inventory>();
        if (initialAmount <= 0) initialAmount = Mathf.Max(1, amount);
        SetTaken(false);
    }

    private void Update()
    {
        if (taken && respawnSeconds > 0f && Time.time >= readyTime)
        {
            amount = initialAmount;
            SetTaken(false);
        }
    }

    public override string Prompt
    {
        get
        {
            string label = KindLabel(kind);
            bool noRoom = inventory != null && inventory.FreeSpaceFor(kind) <= 0;
            return noRoom ? $"줍기 — {label} x{amount} (가득 참)" : $"줍기 — {label} x{amount}";
        }
    }

    public override bool CanInteract(GameObject interactor) => !taken && amount > 0;

    public override void Interact(GameObject interactor)
    {
        if (taken) return;
        if (inventory == null) inventory = FindObjectOfType<Inventory>();
        if (inventory == null) return;

        int stored = inventory.Add(kind, amount);
        if (stored <= 0)
        {
            Debug.Log("[줍기] 자리 없음 — 부리고 오세요");
            return;
        }

        amount -= stored;
        if (amount > 0) return; // 일부만 주움 — 바닥에 남음

        if (respawnSeconds > 0f)
        {
            readyTime = Time.time + respawnSeconds;
            SetTaken(true);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void SetTaken(bool value)
    {
        taken = value;

        if (visual != null) { visual.SetActive(!value); return; }

        Renderer[] rs = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rs.Length; i++) rs[i].enabled = !value;
    }

    private static string KindLabel(ResourceKind kind)
    {
        switch (kind)
        {
            case ResourceKind.Stick: return "나뭇가지";
            case ResourceKind.Stone: return "돌";
            case ResourceKind.Firewood: return "장작";
            case ResourceKind.Food: return "식량";
            case ResourceKind.Axe: return "도끼";
            default: return kind.ToString();
        }
    }
}