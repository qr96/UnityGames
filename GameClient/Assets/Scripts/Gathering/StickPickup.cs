using UnityEngine;

// 맨손 나뭇가지 줍기.
// Respawn Seconds <= 0 : 1회성(주우면 제거)
// Respawn Seconds >  0 : 주우면 숨었다가 그 시간 뒤 재생
// 소지 상한이 가득이면 줍지 못하고 노드도 소모되지 않음(라벨에 표시).
public class StickPickup : InteractableBase
{
    [SerializeField] private int amount = 1;

    [Header("재생")]
    [Tooltip("0 이하면 1회성(제거). 양수면 이 시간(초) 뒤 다시 생김")]
    [SerializeField] private float respawnSeconds = 8f;

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
        if (taken && respawnSeconds > 0f && Time.time >= readyTime)
            SetTaken(false);
    }

    public override string Prompt
        => (inventory != null && inventory.FreeSpaceFor(ResourceKind.Stick) <= 0) ? "줍기 (가득 참)" : "줍기";

    public override bool CanInteract(GameObject interactor) => !taken;

    public override void Interact(GameObject interactor)
    {
        if (taken) return;
        if (inventory == null) inventory = FindObjectOfType<Inventory>();

        int stored = inventory != null ? inventory.Add(ResourceKind.Stick, amount) : 0;
        if (stored <= 0)
        {
            Debug.Log("[줍기] 소지 상한 가득 — 부리고 오세요");
            return; // 노드 소모 없음
        }

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

        if (visual != null)
        {
            visual.SetActive(!value);
            return;
        }

        Renderer[] rs = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rs.Length; i++) rs[i].enabled = !value;
    }
}