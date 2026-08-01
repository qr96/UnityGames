using UnityEngine;

// 쿨타임 채취 노드. 코드 1벌 + 인스펙터 값으로 분화:
//  - 나무(벌목): requiresAxe=true, yield=Firewood, yieldMode=Drop  → 바닥에 떨어뜨림
//  - 열매(채집): requiresAxe=false, yield=Food,   yieldMode=Instant → 즉시 수납
// cooldownSeconds <= 0 이면 1회성(채취 후 제거).
public class HarvestNode : InteractableBase, IHittable
{
    public enum YieldMode { Instant, Drop }

    [Header("산출")]
    [SerializeField] private ResourceKind yieldKind = ResourceKind.Firewood;
    [Tooltip("1회 수확으로 나오는 개수")]
    [SerializeField] private int yieldAmount = 1;
    [Tooltip("소진되기까지 수확할 수 있는 '횟수'. 1이면 한 번 수확하고 소진")]
    [SerializeField] private int harvestCharges = 1;
    [SerializeField] private string prompt = "패기";
    [SerializeField] private YieldMode yieldMode = YieldMode.Instant;

    [Header("드롭 (yieldMode = Drop)")]
    [Tooltip("DroppedItem이 붙은 프리팹")]
    [SerializeField] private GameObject dropPrefab;
    [Tooltip("한 덩이당 수량. 산출량을 이 단위로 나눠 떨어뜨림")]
    [SerializeField] private int amountPerDrop = 1;
    [Tooltip("노드 주변에 흩어지는 반경(물리 없음, 위치만)")]
    [SerializeField] private float scatterRadius = 1.2f;

    [Header("도구")]
    [SerializeField] private bool requiresAxe = true;

    [Header("타격 (1회 수확에 필요한 E 연타)")]
    [Tooltip("수확 1회를 완성하는 데 필요한 E 입력 수. 위의 '채취 횟수'와 별개")]
    [SerializeField] private int hitsRequired = 1;

    public bool RequiresAxe => requiresAxe;
    public int HitsRequired => Mathf.Max(1, hitsRequired);
    public int HitsDone => hits;

    [Header("재생")]
    [Tooltip("채취 후 재생까지 시간(초). 0 이하면 1회성")]
    [SerializeField] private float cooldownSeconds = 5f;

    [Header("비주얼 (선택)")]
    [SerializeField] private GameObject fullVisual;     // 정상
    [SerializeField] private GameObject depletedVisual; // 소진(그루터기 등)

    [Header("타격 효과 (선택)")]
    [Tooltip("비우면 같은 오브젝트에서 찾음")]
    [SerializeField] private HitFeedback hitFeedback;

    [SerializeField] private Inventory inventory; // 비우면 씬에서 찾음

    private float readyTime;
    private bool depleted;
    private int hits;
    private int chargesLeft;

    protected override void OnEnable()
    {
        base.OnEnable();
        HittableRegistry.Register(this);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        HittableRegistry.Unregister(this);
    }

    private void Start()
    {
        if (inventory == null) inventory = FindObjectOfType<Inventory>();
        if (hitFeedback == null) hitFeedback = GetComponent<HitFeedback>();
        SetDepleted(false);
    }

    // ---- IHittable ----
    public Vector3 HitPosition => transform.position;
    public bool CanBeHit => !depleted;
    // 도끼 대상(나무)만 서로 번지게 분류를 나눔
    public HitCategory Category => requiresAxe ? HitCategory.Tree : HitCategory.Other;

    private void Update()
    {
        if (depleted && cooldownSeconds > 0f && Time.time >= readyTime)
            SetDepleted(false);
    }

    // 드롭 방식은 인벤토리가 가득 차도 채취 가능(바닥에 쌓임)
    public override string Prompt
    {
        get
        {
            if (yieldMode == YieldMode.Instant &&
                inventory != null && inventory.FreeSpaceFor(yieldKind) <= 0)
                return $"{prompt} (가득 참)";

            int need = HitsRequired;
            string text = need > 1 ? $"{prompt} ({hits}/{need})" : prompt;
            if (harvestCharges > 1) text += $" [{chargesLeft}회 남음]";
            return text;
        }
    }

    // 도끼 대상(나무)은 E 상호작용이 아니라 공격(AttackExecutor)으로만 맞는다.
    public override bool CanInteract(GameObject interactor)
        => !depleted && !requiresAxe;

    // 채집(도끼 불필요) — E 한 번이 1타
    public override void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor)) return;
        ApplyHits(1, interactor);
    }

    // 타격 누적 → 필요 횟수에 도달하면 산출
    public void ApplyHits(int count, GameObject interactor)
    {
        if (depleted || count <= 0) return;

        // 타격 표시 (흔들림 + 파편)
        if (hitFeedback == null) hitFeedback = GetComponent<HitFeedback>();
        if (hitFeedback != null)
        {
            Vector3 dir = interactor != null
                ? transform.position - interactor.transform.position
                : Vector3.zero;
            hitFeedback.Play(dir);
        }

        hits += count;
        if (hits < HitsRequired) return;

        if (inventory == null) inventory = FindObjectOfType<Inventory>();

        if (yieldMode == YieldMode.Instant)
        {
            int stored = inventory != null ? inventory.Add(yieldKind, yieldAmount) : 0;
            if (stored <= 0)
            {
                hits = HitsRequired - 1; // 마지막 타격 취소 — 다시 시도 가능
                Debug.Log("[채취] 자리 없음 — 부리고 오세요");
                return;
            }
        }
        else
        {
            if (!SpawnDrops())
            {
                hits = HitsRequired - 1;
                return;
            }
        }

        // 채취 횟수 차감 — 남아 있으면 소진하지 않고 계속 수확 가능
        chargesLeft--;
        if (chargesLeft > 0)
        {
            hits = 0;
            return;
        }

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

    private bool SpawnDrops()
    {
        if (dropPrefab == null)
        {
            Debug.LogWarning($"[채취] {name}: Yield Mode가 Drop인데 Drop Prefab이 없음");
            return false;
        }

        int per = Mathf.Max(1, amountPerDrop);
        int remain = Mathf.Max(1, yieldAmount);

        while (remain > 0)
        {
            int chunk = Mathf.Min(per, remain);
            remain -= chunk;

            Vector2 c = Random.insideUnitCircle * scatterRadius;
            Vector3 pos = transform.position + new Vector3(c.x, 0f, c.y);

            GameObject go = Instantiate(dropPrefab, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            DroppedItem drop = go.GetComponent<DroppedItem>();
            if (drop != null) drop.Setup(yieldKind, chunk);
            else Debug.LogWarning("[채취] Drop Prefab에 DroppedItem 없음");
        }

        return true;
    }

    private void SetDepleted(bool value)
    {
        depleted = value;
        hits = 0;                                  // 타격 진행 초기화
        if (!value) chargesLeft = Mathf.Max(1, harvestCharges); // 재생 시 채취 횟수 복구
        if (fullVisual != null) fullVisual.SetActive(!value);
        if (depletedVisual != null) depletedVisual.SetActive(value);
    }
}