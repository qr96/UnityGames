using UnityEngine;

// 쿨타임 채취 노드. 코드 1벌 + 인스펙터 값으로 분화:
//  - 나무(벌목): requiresAxe=true, yield=Firewood, cooldown>0 (그루터기 → 재생)
//  - 열매(채집): requiresAxe=false, yield=Food
// cooldownSeconds <= 0 이면 1회성(채취 후 제거).
public class HarvestNode : InteractableBase
{
    [Header("산출")]
    [SerializeField] private ResourceKind yieldKind = ResourceKind.Firewood;
    [SerializeField] private int yieldAmount = 1;
    [SerializeField] private string prompt = "패기";

    [Header("도구")]
    [SerializeField] private bool requiresAxe = true;

    [Header("재생")]
    [Tooltip("채취 후 재생까지 시간(초). 0 이하면 1회성")]
    [SerializeField] private float cooldownSeconds = 5f;

    [Header("비주얼 (선택)")]
    [SerializeField] private GameObject fullVisual;     // 정상
    [SerializeField] private GameObject depletedVisual; // 소진(그루터기 등)

    [SerializeField] private Inventory inventory; // 비우면 씬에서 찾음

    private float readyTime;
    private bool depleted;

    private void Start()
    {
        if (inventory == null) inventory = FindObjectOfType<Inventory>();
        SetDepleted(false);
    }

    private void Update()
    {
        if (depleted && cooldownSeconds > 0f && Time.time >= readyTime)
            SetDepleted(false);
    }

    public override string Prompt => prompt;

    public override bool CanInteract(GameObject interactor)
    {
        if (depleted) return false;
        if (requiresAxe)
        {
            PlayerTools tools = interactor.GetComponent<PlayerTools>();
            if (tools == null || !tools.HasAxe) return false;
        }
        return true;
    }

    public override void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor)) return;

        if (inventory == null) inventory = FindObjectOfType<Inventory>();
        if (inventory != null) inventory.Add(yieldKind, yieldAmount);

        if (cooldownSeconds > 0f)
        {
            readyTime = Time.time + cooldownSeconds;
            SetDepleted(true);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void SetDepleted(bool value)
    {
        depleted = value;
        if (fullVisual != null) fullVisual.SetActive(!value);
        if (depletedVisual != null) depletedVisual.SetActive(value);
    }
}
