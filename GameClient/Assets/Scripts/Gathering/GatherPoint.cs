using UnityEngine;

// 월드에 놓인 채집물(나뭇가지·잔돌·베리). E로 즉시 인벤토리에 들어가고 일정 시간 뒤 리스폰.
// 부순 뒤 나오는 바닥 드랍(DroppedItem)과는 별개다.
public class GatherPoint : InteractableBase
{
    [SerializeField] private ResourceKind kind = ResourceKind.Stick;
    [SerializeField] private int amount = 1;
    [SerializeField] private string prompt = "따기";

    [Header("리스폰")]
    [Tooltip("0 이하면 재생 없이 제거")]
    [SerializeField] private float respawnSeconds = 30f;

    [Header("비주얼 (선택) — 비우면 렌더러를 껐다 켬")]
    [SerializeField] private GameObject visual;

    [SerializeField] private Inventory inventory; // 비우면 씬에서 찾음

    private bool taken;
    private float readyTime;

    private void Start()
    {
        if (inventory == null) inventory = FindObjectOfType<Inventory>();
        SetTaken(false);
    }

    private void Update()
    {
        if (taken && respawnSeconds > 0f && Time.time >= readyTime) SetTaken(false);
    }

    public override string Prompt
    {
        get
        {
            string label = PlayerCrafting.KindLabel(kind);
            bool full = inventory != null && inventory.FreeSpaceFor(kind) <= 0;
            return full ? $"{prompt} — {label} (가득 참)" : $"{prompt} — {label}";
        }
    }

    public override bool CanInteract(GameObject interactor) => !taken;

    public override void Interact(GameObject interactor)
    {
        if (taken) return;
        if (inventory == null) inventory = FindObjectOfType<Inventory>();

        int stored = inventory != null ? inventory.Add(kind, amount) : 0;
        if (stored <= 0)
        {
            Debug.Log("[따기] 자리 없음 — 부리고 오세요");
            return;
        }

        if (respawnSeconds > 0f)
        {
            readyTime = Time.time + respawnSeconds;
            SetTaken(true);
        }
        else Destroy(gameObject);
    }

    private void SetTaken(bool value)
    {
        taken = value;
        if (visual != null) { visual.SetActive(!value); return; }

        Renderer[] rs = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rs.Length; i++) rs[i].enabled = !value;
    }
}
