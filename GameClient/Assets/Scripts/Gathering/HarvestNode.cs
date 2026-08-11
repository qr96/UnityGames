using UnityEngine;

// 쿨타임 채취 노드. 코드 1벌 + 인스펙터 값으로 분화:
//  - 도구로 캐는 노드(나무·바위): requiresAxe=true. HP가 있고 스윙 1회에 도구 위력만큼 깎인다.
//  - 손으로 따는 노드(베리 등): requiresAxe=false. E 한 번에 즉시 수확(HP 무시).
// cooldownSeconds <= 0 이면 1회성(채취 후 제거).
public class HarvestNode : InteractableBase, IHittable
{
    public enum YieldMode { Instant, Drop }

    [System.Serializable]
    public struct Yield
    {
        public ItemDef item;
        public int amount;
    }

    [Header("산출 (여러 종류 가능)")]
    [Tooltip("1회 수확으로 나오는 것들. 예: 장작 2 + 잔가지 1")]
    [SerializeField] private Yield[] yields = new Yield[0];
    [Tooltip("소진되기까지 수확할 수 있는 횟수. 3이면 E를 세 번 눌러 세 번 수확한다")]
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

    [Header("내구도 (도구로 캐는 노드만)")]
    [Tooltip("노드 HP. 스윙 1회에 도구 위력(hitPower)만큼 깎이고 0이 되면 산출된다. " +
             "상위 도구일수록 적은 타수로 넘어간다. 손으로 채집하는 노드에서는 무시된다")]
    [SerializeField] private int nodeHealth = 3;

    public bool RequiresAxe => requiresAxe;
    public int MaxHealth => Mathf.Max(1, nodeHealth);
    public int CurrentHealth => Mathf.Max(0, MaxHealth - damage);

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
    private int damage;        // 누적 피해
    private int chargesLeft;   // 남은 수확 횟수

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
            if (yieldMode == YieldMode.Instant && inventory != null && yields != null &&
                yields.Length > 0 && inventory.FreeSpaceFor(yields[0].item) <= 0)
                return $"{prompt} (가득 참)";

            string text = prompt;

            // 도구 노드는 남은 HP, 손 채집은 진행 표시 없음
            if (requiresAxe && damage > 0) text = $"{prompt} ({CurrentHealth}/{MaxHealth})";

            if (harvestCharges > 1) text += $" [{chargesLeft}회 남음]";
            return text;
        }
    }

    // 도끼 대상(나무)은 E 상호작용이 아니라 공격(AttackExecutor)으로만 맞는다.
    public override bool CanInteract(GameObject interactor)
        => !depleted && !requiresAxe;

    // 손 채집(도구 불필요) — HP와 무관하게 E 한 번에 즉시 수확
    public override void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor)) return;
        ApplyHits(MaxHealth, interactor);
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

        damage += count;                  // count = 도구 위력
        if (damage < MaxHealth) return;

        if (inventory == null) inventory = FindObjectOfType<Inventory>();

        if (yieldMode == YieldMode.Instant)
        {
            if (!GiveYieldsToInventory())
            {
                damage = MaxHealth - 1; // 마지막 타격 취소 — 다시 시도 가능
                Debug.Log("[채취] 자리 없음 — 부리고 오세요");
                return;
            }
        }
        else
        {
            if (!SpawnDrops())
            {
                damage = MaxHealth - 1;
                return;
            }
        }

        damage = 0;

        // 수확 횟수 차감 — 남아 있으면 소진하지 않는다
        chargesLeft--;
        if (chargesLeft > 0) return;

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

    // 즉시 수납 — 하나라도 들어가면 성공
    private bool GiveYieldsToInventory()
    {
        if (inventory == null)
        {
            Debug.LogWarning($"[채취] {name}: Inventory를 찾지 못함");
            return false;
        }

        if (yields == null || yields.Length == 0)
        {
            Debug.LogWarning($"[채취] {name}: Yields 배열이 비어 있음 — 인스펙터에서 산출물을 지정할 것");
            return false;
        }

        bool any = false;
        for (int i = 0; i < yields.Length; i++)
        {
            if (yields[i].amount <= 0)
            {
                Debug.LogWarning($"[채취] {name}: Yields[{i}] 수량이 0");
                continue;
            }

            int stored = inventory.Add(yields[i].item, yields[i].amount);
            if (stored > 0)
            {
                any = true;
                Debug.Log($"[채취] {name}: {yields[i].item.displayName} {stored}개 획득");
            }
            else
            {
                Debug.Log($"[채취] {name}: {(yields[i].item != null ? yields[i].item.displayName : "미지정")} " +
                          "수납 실패 (칸 부족이거나 Yields 미설정)");
            }
        }
        return any;
    }

    private bool SpawnDrops()
    {
        if (dropPrefab == null)
        {
            Debug.LogWarning($"[채취] {name}: Yield Mode가 Drop인데 Drop Prefab이 없음");
            return false;
        }
        if (yields == null || yields.Length == 0) return false;

        int per = Mathf.Max(1, amountPerDrop);

        for (int y = 0; y < yields.Length; y++)
        {
            int remain = yields[y].amount;
            while (remain > 0)
            {
                int chunk = Mathf.Min(per, remain);
                remain -= chunk;

                Vector2 c = Random.insideUnitCircle * scatterRadius;
                Vector3 pos = transform.position + new Vector3(c.x, 0f, c.y);

                GameObject go = Instantiate(dropPrefab, pos,
                    Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                DroppedItem drop = go.GetComponent<DroppedItem>();
                if (drop != null) drop.Setup(yields[y].item, chunk);
                else Debug.LogWarning("[채취] Drop Prefab에 DroppedItem 없음");
            }
        }

        return true;
    }

    private void SetDepleted(bool value)
    {
        depleted = value;
        damage = 0;                                       // 재생·소진 시 피해 초기화
        if (!value) chargesLeft = Mathf.Max(1, harvestCharges); // 재생 시 수확 횟수 복구
        if (fullVisual != null) fullVisual.SetActive(!value);
        if (depletedVisual != null) depletedVisual.SetActive(value);
    }
}