using UnityEngine;

// 개발용 식별(throwaway). 오브젝트에 붙은 컴포넌트 종류를 보고 색과 높이를 자동 지정.
// 모델이 들어오면 이 컴포넌트만 제거하면 됨. 씬의 아무 오브젝트에 하나 붙여도 되고,
// applyToAllInScene = true 로 두면 시작 시 씬 전체를 한 번에 칠함.
public class DevTint : MonoBehaviour
{
    [SerializeField] private bool applyToAllInScene = true;

    private void Start()
    {
        if (applyToAllInScene)
        {
            InteractableBase[] all = FindObjectsOfType<InteractableBase>();
            for (int i = 0; i < all.Length; i++) Apply(all[i].gameObject);

            DroppedItem[] drops = FindObjectsOfType<DroppedItem>();
            for (int i = 0; i < drops.Length; i++) Apply(drops[i].gameObject);

            Hearth[] hearths = FindObjectsOfType<Hearth>();
            for (int i = 0; i < hearths.Length; i++) Apply(hearths[i].gameObject);
        }
        else
        {
            Apply(gameObject);
        }
    }

    private static void Apply(GameObject go)
    {
        Color color;
        float height;

        if (go.GetComponent<Hearth>() != null) { color = new Color(1.0f, 0.45f, 0.1f); height = 1.0f; } // 주황: 화로
        else if (go.GetComponent<CraftingStation>() != null) { color = new Color(0.6f, 0.4f, 0.2f); height = 1.2f; } // 갈색: 제작대
        else if (go.GetComponent<Merchant>() != null) { color = new Color(0.9f, 0.8f, 0.2f); height = 1.8f; } // 노랑: 행상인
        else if (go.GetComponent<DroppedItem>() != null || go.GetComponent<GatherPoint>() != null)
        {
            DroppedItem d = go.GetComponent<DroppedItem>();
            ResourceKind kind = d != null ? d.Kind : ResourceKind.Stick;
            switch (kind)
            {
                case ResourceKind.Stone: color = new Color(0.55f, 0.55f, 0.6f); height = 0.3f; break; // 회색: 돌
                case ResourceKind.Firewood: color = new Color(0.45f, 0.3f, 0.15f); height = 0.4f; break; // 짙은 갈색: 장작
                case ResourceKind.Food: color = new Color(0.8f, 0.2f, 0.35f); height = 0.3f; break;
                default: color = new Color(0.75f, 0.65f, 0.45f); height = 0.3f; break; // 베이지: 나뭇가지
            }
        }
        else if (go.GetComponent<HarvestNode>() != null)
        {
            // 도끼 필요 여부로 나무/열매 구분
            HarvestNode node = go.GetComponent<HarvestNode>();
            bool axe = node.RequiresAxe;
            if (axe) { color = new Color(0.15f, 0.45f, 0.2f); height = 2.5f; }  // 진초록·큼: 나무
            else { color = new Color(0.8f, 0.2f, 0.35f); height = 0.6f; }   // 붉은색·작음: 열매
        }
        else return;

        // 색
        Renderer[] rs = go.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rs.Length; i++)
        {
            Material m = rs[i].material; // 인스턴스 생성(개발용이라 무방)
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color); // URP
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        }

        // 높이(가로는 유지, 세로만 조정 → 실루엣으로 구분)
        Vector3 s = go.transform.localScale;
        go.transform.localScale = new Vector3(s.x, height, s.z);
    }

}