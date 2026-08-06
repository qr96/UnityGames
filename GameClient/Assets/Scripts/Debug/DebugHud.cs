using UnityEngine;

// 임시 개발용 오버레이(throwaway). 정식 UI는 별도.
// 타겟 / 도구 / 자원 / 스탯 표시. G키 = 디버그 도끼 지급(테스트용).
public class DebugHud : MonoBehaviour
{
    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private AttackExecutor attack;
    [SerializeField] private Inventory inventory;
    [SerializeField] private PlayerStats stats;
    [SerializeField] private PickupCollector collector;

    private GUIStyle style;

    private void Update()
    {
        // 디버그 도끼 지급 (정식 경로는 제작대)
        if (Input.GetKeyDown(KeyCode.G) && inventory != null)
        {
            if (inventory.Add(ResourceKind.Axe, 1) <= 0)
                Debug.Log("[디버그] 도끼를 넣을 칸 없음 / ItemDef 미등록");
        }
    }

    private void Start()
    {
        if (interactor == null) interactor = FindObjectOfType<PlayerInteractor>();
        if (attack == null) attack = FindObjectOfType<AttackExecutor>();
        if (inventory == null) inventory = FindObjectOfType<Inventory>();
        if (stats == null) stats = FindObjectOfType<PlayerStats>();
        if (collector == null) collector = FindObjectOfType<PickupCollector>();
    }

    private void OnGUI()
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { fontSize = 16 };
            style.normal.textColor = Color.white;
        }

        var sb = new System.Text.StringBuilder();

        string prompt = (interactor != null && interactor.Current != null)
            ? interactor.Current.Prompt
            : "없음";
        sb.AppendLine($"타겟: {prompt}   (E: 상호작용)");

        if (attack != null)
        {
            ItemDef tool = attack.EquippedTool;
            sb.AppendLine(tool != null
                ? $"장비(퀵슬롯): {tool.displayName} (위력 {tool.hitPower})   (G: 도끼 지급)"
                : "장비(퀵슬롯): 없음   (G: 도끼 지급)");
        }

        if (inventory != null)
        {
            sb.AppendLine($"나뭇가지 {inventory.Get(ResourceKind.Stick)}   돌 {inventory.Get(ResourceKind.Stone)}");
            sb.AppendLine($"장작 {inventory.Get(ResourceKind.Firewood)}   식량 {inventory.Get(ResourceKind.Food)}");
            sb.AppendLine($"골드 {inventory.Gold}");
        }

        if (collector != null && collector.NearbyCount > 0)
            sb.AppendLine($"바닥 아이템 {collector.NearbyCount}묶음 — F로 줍기");

        if (stats != null)
            sb.AppendLine($"온기 {stats.Warmth:0}   허기 {stats.Hunger:0}   {(stats.IsDown ? "[쓰러짐]" : "")}");

        GUI.Box(new Rect(10, 10, 300, 190), GUIContent.none);
        GUI.Label(new Rect(20, 16, 280, 180), sb.ToString(), style);
    }
}