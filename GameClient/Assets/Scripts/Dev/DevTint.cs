using UnityEngine;

// 개발용 식별(throwaway). 붙어 있는 컴포넌트 종류를 보고 색과 높이를 자동 지정한다.
// 씬에 하나만 두고 Apply To All In Scene을 켜면 씬 전체를 한 번에 칠한다.
// 모델·아트가 들어오면 이 컴포넌트를 씬에서 빼면 된다.
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

        if (go.GetComponent<Hearth>() != null)
        {
            color = new Color(1.0f, 0.45f, 0.1f); height = 1.0f;      // 주황: 화로
        }
        else if (go.GetComponent<CraftingStation>() != null)
        {
            color = new Color(0.6f, 0.4f, 0.2f); height = 1.2f;       // 갈색: 제작 시설
        }
        else if (go.GetComponent<Merchant>() != null)
        {
            color = new Color(0.9f, 0.8f, 0.2f); height = 1.8f;       // 노랑: 행상인
        }
        else if (go.GetComponent<DroppedItem>() != null)
        {
            DroppedItem d = go.GetComponent<DroppedItem>();
            ItemColor(d.Item != null ? d.Item.id : "", out color, out height);
        }
        else if (go.GetComponent<ResourceSource>() != null)
        {
            ResourceSourceDef def = go.GetComponent<ResourceSource>().Def;

            if (def == null)
            {
                color = Color.magenta; height = 1f;                   // 자홍: Def 미지정
            }
            else if (!def.IsHandGathered)
            {
                color = new Color(0.15f, 0.45f, 0.2f); height = 2.5f; // 진초록·큼: 도구로 캐는 것
            }
            else
            {
                string id = (def.yields != null && def.yields.Length > 0 && def.yields[0].item != null)
                    ? def.yields[0].item.id : "";
                ItemColor(id, out color, out height);
            }
        }
        else return;

        Renderer[] rs = go.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rs.Length; i++)
        {
            Material m = rs[i].material; // 인스턴스 생성(개발용이라 무방)
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        }

        // 가로는 유지, 세로만 조정 → 실루엣으로 구분
        Vector3 s = go.transform.localScale;
        go.transform.localScale = new Vector3(s.x, height, s.z);
    }

    // 아이템 id로 색·높이 결정
    private static void ItemColor(string id, out Color color, out float height)
    {
        if (id.Contains("stone")) { color = new Color(0.55f, 0.55f, 0.6f); height = 0.3f; }
        else if (id.Contains("firewood") || id.Contains("log")) { color = new Color(0.45f, 0.3f, 0.15f); height = 0.4f; }
        else if (id.Contains("berry") || id.Contains("food")) { color = new Color(0.8f, 0.2f, 0.35f); height = 0.5f; }
        else { color = new Color(0.75f, 0.65f, 0.45f); height = 0.3f; }
    }
}