using UnityEngine;

// 배치된 자원원 하나. 밸런스는 Def에, 여기에는 인스턴스 상태만 있다.
//  - 맨손 수확(requiredTool = None): E 한 번에 즉시
//  - 도구 수확: 그 도구를 들고 스윙 → Def.health를 도구 위력만큼 깎아 0에서 산출
// 세이브는 uid를 키로 damage·chargesLeft·regenAt만 저장한다.
public class ResourceSource : InteractableBase, IHittable
{
    [Header("정의")]
    [SerializeField] private ResourceSourceDef def;

    [Header("인스턴스")]
    [Tooltip("맵 파일에서 주입되는 세이브 키. 손으로 배치했으면 비워도 된다")]
    [SerializeField] private string uid;
    [Tooltip("시각 변형만 — 밸런스에는 영향 없음")]
    [SerializeField] private int variant;

    [Header("비주얼 (선택)")]
    [SerializeField] private GameObject fullVisual;
    [SerializeField] private GameObject depletedVisual;

    [Header("참조 (비우면 씬에서 찾음)")]
    [SerializeField] private Inventory inventory;
    [SerializeField] private HitFeedback hitFeedback;

    private int damage;
    private int chargesLeft;
    private bool depleted;
    private double regenAt;

    public ResourceSourceDef Def => def;
    public string Uid => uid;
    public int Variant => variant;
    public bool IsDepleted => depleted;
    public int ChargesLeft => chargesLeft;
    public int CurrentHealth => def != null ? Mathf.Max(0, def.Health - damage) : 0;

    // ---- 생성·복원 ----
    public void Setup(ResourceSourceDef newDef, string newUid, int newVariant = 0)
    {
        def = newDef;
        uid = newUid;
        variant = newVariant;
        ResetState();
    }

    // 세이브 복원용
    public void LoadState(int savedDamage, int savedCharges, double savedRegenAt)
    {
        damage = savedDamage;
        chargesLeft = savedCharges;
        regenAt = savedRegenAt;
        SetDepleted(chargesLeft <= 0);
    }

    private void ResetState()
    {
        damage = 0;
        chargesLeft = def != null ? def.Charges : 1;
        SetDepleted(false);
    }

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

        if (def == null)
        {
            Debug.LogWarning($"[자원원] {name}: Def가 지정되지 않음");
            return;
        }

        if (chargesLeft <= 0 && !depleted) ResetState();
        else SetDepleted(depleted);
    }

    private void Update()
    {
        if (!depleted || def == null) return;
        if (def.regenSeconds <= 0f) return;

        if (GameClock.Time_ >= regenAt) ResetState();
    }

    // ---- IHittable ----
    public Vector3 HitPosition => transform.position;
    public bool CanBeHit => !depleted && def != null && !def.IsHandGathered;
    public HitCategory Category => HitCategory.Tree;   // 자원원 계열은 서로 번지게 같은 분류

    // ---- 상호작용(맨손 채집만) ----
    public override string Prompt
    {
        get
        {
            if (def == null) return "자원";

            string text = def.prompt;
            if (!def.IsHandGathered && damage > 0)
                text += $" ({CurrentHealth}/{def.Health})";
            if (def.Charges > 1) text += $" [{chargesLeft}회 남음]";
            return text;
        }
    }

    // 도구가 필요한 자원원은 E 대상이 아니다(스윙 전용)
    public override bool CanInteract(GameObject interactor)
        => !depleted && def != null && def.IsHandGathered;

    public override void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor)) return;
        Harvest(interactor);
    }

    // ---- 타격 ----
    public void ApplyHits(int count, GameObject attacker)
    {
        if (depleted || def == null || count <= 0) return;
        if (def.IsHandGathered) return;   // 맨손 자원원은 스윙 대상이 아니다

        PlayHitFeedback(attacker);

        damage += count;   // count = 도구 위력
        if (damage < def.Health) return;

        Harvest(attacker);
    }

    // ---- 수확 처리 ----
    private void Harvest(GameObject actor)
    {
        if (inventory == null) inventory = FindObjectOfType<Inventory>();

        bool ok = def.yieldMode == ResourceSourceDef.YieldMode.Instant
            ? GiveToInventory()
            : SpawnDrops();

        if (!ok)
        {
            damage = Mathf.Max(0, def.Health - 1);   // 마지막 타격 취소 — 다시 시도 가능
            return;
        }

        damage = 0;
        chargesLeft--;

        if (chargesLeft > 0) return;

        if (def.regenSeconds > 0f)
        {
            regenAt = GameClock.Time_ + def.regenSeconds;
            SetDepleted(true);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private bool GiveToInventory()
    {
        if (inventory == null)
        {
            Debug.LogWarning($"[자원원] {name}: Inventory를 찾지 못함");
            return false;
        }
        if (def.yields == null || def.yields.Length == 0)
        {
            Debug.LogWarning($"[자원원] {def.displayName}: Yields가 비어 있음");
            return false;
        }

        bool any = false;
        for (int i = 0; i < def.yields.Length; i++)
        {
            ResourceSourceDef.Yield y = def.yields[i];
            if (y.item == null || y.amount <= 0) continue;
            if (inventory.Add(y.item, y.amount) > 0) any = true;
        }

        if (!any) Debug.Log($"[자원원] {def.displayName}: 자리 없음 — 부리고 오세요");
        return any;
    }

    private bool SpawnDrops()
    {
        if (def.dropPrefab == null)
        {
            Debug.LogWarning($"[자원원] {def.displayName}: Drop Prefab이 없음");
            return false;
        }
        if (def.yields == null || def.yields.Length == 0)
        {
            Debug.LogWarning($"[자원원] {def.displayName}: Yields가 비어 있음");
            return false;
        }

        int per = Mathf.Max(1, def.amountPerDrop);

        for (int y = 0; y < def.yields.Length; y++)
        {
            ResourceSourceDef.Yield yield = def.yields[y];
            if (yield.item == null) continue;

            int remain = yield.amount;
            while (remain > 0)
            {
                int chunk = Mathf.Min(per, remain);
                remain -= chunk;

                Vector2 c = Random.insideUnitCircle * def.scatterRadius;
                Vector3 pos = transform.position + new Vector3(c.x, 0f, c.y);

                GameObject go = Instantiate(def.dropPrefab, pos,
                    Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));

                DroppedItem drop = go.GetComponent<DroppedItem>();
                if (drop != null) drop.Setup(yield.item, chunk);
                else Debug.LogWarning($"[자원원] {def.displayName}: Drop Prefab에 DroppedItem 없음");
            }
        }
        return true;
    }

    private void PlayHitFeedback(GameObject attacker)
    {
        if (hitFeedback == null) hitFeedback = GetComponent<HitFeedback>();
        if (hitFeedback == null) return;

        Vector3 dir = attacker != null
            ? transform.position - attacker.transform.position
            : Vector3.zero;
        hitFeedback.Play(dir);
    }

    private void SetDepleted(bool value)
    {
        depleted = value;
        if (fullVisual != null) fullVisual.SetActive(!value);
        if (depletedVisual != null) depletedVisual.SetActive(value);
    }
}