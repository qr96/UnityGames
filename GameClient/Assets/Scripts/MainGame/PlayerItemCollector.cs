using UnityEngine;

public class PlayerItemCollector : MonoBehaviour
{
    [Header("감지")]
    public float collectRange = 2.5f;
    public LayerMask itemLayer;

    [Header("기준점")]
    [SerializeField] Transform _collectOrigin; // 비워두면 transform.position 사용

    Vector3 CollectOrigin => _collectOrigin != null ? _collectOrigin.position : transform.position;

    void Update()
    {
        var hits = Physics.OverlapSphere(CollectOrigin, collectRange, itemLayer);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<DroppedItem>(out var item))
                item.Attract(transform);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        var origin = _collectOrigin != null ? _collectOrigin.position : transform.position;
        Gizmos.DrawWireSphere(origin, collectRange);
    }
}
