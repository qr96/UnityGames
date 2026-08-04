using System.Collections.Generic;
using UnityEngine;

// 제작 시설(제작대·모루·요리솥). 자신을 등록해 두고, 근처 판정에만 쓰인다.
// 제작 실행은 PlayerCrafting이 담당하며, 이 컴포넌트는 "무슨 시설이 어디 있는지"만 알린다.
public class CraftingStation : MonoBehaviour
{
    public static readonly List<CraftingStation> All = new List<CraftingStation>();

    [SerializeField] private CraftStation stationType = CraftStation.Workbench;
    [Tooltip("이 거리 안이면 해당 시설을 쓸 수 있다")]
    [SerializeField] private float useRadius = 3f;

    public CraftStation StationType => stationType;
    public float UseRadius => useRadius;

    private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    private void OnDisable() { All.Remove(this); }

    // 지점 근처에 해당 등급 시설이 있는지 (Hand는 항상 참)
    public static bool IsAvailable(CraftStation type, Vector3 point)
    {
        if (type == CraftStation.Hand) return true;

        for (int i = 0; i < All.Count; i++)
        {
            CraftingStation s = All[i];
            if (s.stationType != type) continue;
            Vector3 d = point - s.transform.position; d.y = 0f;
            if (d.sqrMagnitude <= s.useRadius * s.useRadius) return true;
        }
        return false;
    }

    public static string Label(CraftStation type)
    {
        switch (type)
        {
            case CraftStation.Hand: return "맨손";
            case CraftStation.Workbench: return "제작대";
            case CraftStation.Anvil: return "모루";
            case CraftStation.CookingPot: return "요리솥";
            default: return type.ToString();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0.9f, 1f, 0.8f);
        const int seg = 24;
        Vector3 prev = transform.position + new Vector3(useRadius, 0f, 0f);
        for (int i = 1; i <= seg; i++)
        {
            float a = (i / (float)seg) * Mathf.PI * 2f;
            Vector3 p = transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * useRadius;
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
    }
}