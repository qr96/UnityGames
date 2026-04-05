using UnityEngine;

public class PlayerItemCollector : MonoBehaviour
{
    [Header("감지")]
    public float collectRange = 2.5f;
    public LayerMask itemLayer;

    void Update()
    {
        var hits = Physics.OverlapSphere(transform.position, collectRange, itemLayer);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<DroppedItem>(out var item))
                item.Attract(transform);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, collectRange);
    }
}
