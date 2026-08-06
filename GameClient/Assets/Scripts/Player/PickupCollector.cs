using UnityEngine;

// 바닥 드랍(DroppedItem) 회수. 상호작용과 같은 키(E)를 쓰고, 우선순위로 겹침을 피한다:
//   타겟이 잡혀 있으면 그 상호작용이 먼저(화로·제작대·따기), 타겟이 없을 때만 줍기.
// 기본은 가장 가까운 한 묶음. Collect Mode를 바꾸면 반경 일괄로도 동작한다.
public class PickupCollector : MonoBehaviour
{
    public enum Mode
    {
        Nearest,      // 한 번에 가장 가까운 한 묶음
        AllInRadius,  // 반경 안 전부
    }

    [SerializeField] private KeyCode pickupKey = KeyCode.E;
    [SerializeField] private Mode collectMode = Mode.Nearest;
    [Tooltip("비우면 씬에서 찾음. 타겟이 잡혀 있을 때는 줍지 않는다")]
    [SerializeField] private PlayerInteractor interactor;
    [Tooltip("이 반경 안의 바닥 아이템을 모두 줍는다")]
    [SerializeField] private float radius = 3f;
    [SerializeField] private Inventory inventory; // 비우면 씬에서 찾음

    [Header("안내")]
    [Tooltip("반경 안에 줍을 것이 있으면 화면에 안내 표시")]
    [SerializeField] private bool showPrompt = true;

    // 지금 반경 안에 줍을 것이 있는지 (UI 표시용)
    public int NearbyCount { get; private set; }

    private void Start()
    {
        if (inventory == null) inventory = FindObjectOfType<Inventory>();
        if (interactor == null) interactor = GetComponent<PlayerInteractor>();
        if (interactor == null) interactor = FindObjectOfType<PlayerInteractor>();
    }

    // 상호작용 타겟이 잡혀 있으면 E는 그쪽 몫
    private bool InteractionHasPriority
        => interactor != null && interactor.Current != null;

    private void Update()
    {
        NearbyCount = CountNearby();

        if (!Input.GetKeyDown(pickupKey)) return;

        if (InteractionHasPriority) return; // 화로·제작대·따기가 먼저

        if (UIInputLock.IsBlocked)
        {
            Debug.Log("[줍기] UI가 열려 있어 입력 무시 (Tab/Q/ESC로 닫기)");
            return;
        }

        if (NearbyCount <= 0)
        {
            Debug.Log($"[줍기] 반경 {radius}m 안에 바닥 아이템이 없음 " +
                      $"(씬 전체 {DroppedItem.All.Count}개)");
            return;
        }

        if (collectMode == Mode.Nearest) CollectNearest();
        else CollectAll();
    }

    // 가장 가까운 한 묶음만 회수
    public void CollectNearest()
    {
        if (inventory == null) inventory = FindObjectOfType<Inventory>();
        if (inventory == null) return;

        DroppedItem best = null;
        float bestSqr = radius * radius;

        var list = DroppedItem.All;
        for (int i = 0; i < list.Count; i++)
        {
            DroppedItem d = list[i];
            if (d == null) continue;
            Vector3 v = d.Position - transform.position; v.y = 0f;
            float sqr = v.sqrMagnitude;
            if (sqr <= bestSqr) { bestSqr = sqr; best = d; }
        }

        if (best == null) return;

        ResourceKind kind = best.Kind;
        int got = best.Collect(inventory);

        if (got > 0) Debug.Log($"[줍기] {PlayerCrafting.KindLabel(kind)} {got}개");
        else Debug.Log("[줍기] 자리 없음 — 부리고 오세요");
    }

    private int CountNearby()
    {
        int count = 0;
        float r2 = radius * radius;
        var list = DroppedItem.All;
        for (int i = 0; i < list.Count; i++)
        {
            DroppedItem d = list[i];
            if (d == null) continue;
            Vector3 v = d.Position - transform.position; v.y = 0f;
            if (v.sqrMagnitude <= r2) count++;
        }
        return count;
    }

    public void CollectAll()
    {
        if (inventory == null) inventory = FindObjectOfType<Inventory>();
        if (inventory == null) return;

        float r2 = radius * radius;
        int picked = 0, leftover = 0;

        // 회수 중 목록이 바뀌므로 역순 순회
        var list = DroppedItem.All;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            DroppedItem d = list[i];
            if (d == null) continue;

            Vector3 v = d.Position - transform.position; v.y = 0f;
            if (v.sqrMagnitude > r2) continue;

            int got = d.Collect(inventory);
            if (got > 0) picked += got;
            else leftover++;
        }

        if (picked > 0)
            Debug.Log(leftover > 0
                ? $"[줍기] {picked}개 회수 — 자리가 없어 {leftover}묶음 남음"
                : $"[줍기] {picked}개 회수");
        else if (leftover > 0)
            Debug.Log("[줍기] 자리 없음 — 부리고 오세요");
    }

    private GUIStyle promptStyle;

    private void OnGUI()
    {
        if (!showPrompt || NearbyCount <= 0) return;
        if (InteractionHasPriority) return; // 상호작용 라벨과 겹치지 않게

        if (promptStyle == null)
        {
            promptStyle = new GUIStyle(GUI.skin.box) { fontSize = 15, alignment = TextAnchor.MiddleCenter };
            promptStyle.normal.textColor = Color.white;
        }

        string text = collectMode == Mode.Nearest
            ? $"{pickupKey} — 줍기 (근처 {NearbyCount}묶음)"
            : $"{pickupKey} — 모두 줍기 ({NearbyCount}묶음)";
        GUI.Box(new Rect((Screen.width - 220f) * 0.5f, Screen.height - 110f, 220f, 30f), text, promptStyle);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 1f, 0.7f, 0.8f);
        const int seg = 24;
        Vector3 prev = transform.position + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= seg; i++)
        {
            float a = (i / (float)seg) * Mathf.PI * 2f;
            Vector3 p = transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
    }
}