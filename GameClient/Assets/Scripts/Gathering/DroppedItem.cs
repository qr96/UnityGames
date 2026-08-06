using System.Collections.Generic;
using UnityEngine;

// 부순 뒤 바닥에 떨어진 자원. E 상호작용 대상이 아니고,
// PickupCollector가 반경 일괄 줍기로 회수한다. 방치하면 소멸(눈에 파묻힘).
public class DroppedItem : MonoBehaviour
{
    public static readonly List<DroppedItem> All = new List<DroppedItem>();

    [SerializeField] private ResourceKind kind = ResourceKind.Firewood;
    [SerializeField] private int amount = 1;

    [Header("소멸")]
    [Tooltip("이 시간(초) 뒤 사라짐. 0 이하면 사라지지 않음")]
    [SerializeField] private float despawnSeconds = 120f;

    public ResourceKind Kind => kind;
    public int Amount => amount;
    public Vector3 Position => transform.position;

    private float despawnTime;

    private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    private void OnDisable() { All.Remove(this); }

    // 노드가 스폰 직후 호출
    public void Setup(ResourceKind newKind, int newAmount)
    {
        kind = newKind;
        amount = Mathf.Max(1, newAmount);
    }

    private void Start()
    {
        if (despawnSeconds > 0f) despawnTime = Time.time + despawnSeconds;
    }

    private void Update()
    {
        if (despawnSeconds > 0f && Time.time >= despawnTime) Destroy(gameObject);
    }

    // 수납된 개수 반환. 남으면 바닥에 그대로 남는다.
    public int Collect(Inventory inv)
    {
        if (inv == null || amount <= 0) return 0;

        int stored = inv.Add(kind, amount);
        if (stored <= 0) return 0;

        amount -= stored;
        if (amount <= 0) Destroy(gameObject);
        return stored;
    }
}