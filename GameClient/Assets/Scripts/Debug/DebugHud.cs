using UnityEngine;

// 임시 개발용 오버레이(throwaway). 정식 UI는 StatHUD로 따로.
// 현재 상호작용 타겟 / 도끼 상태 / 인벤토리를 화면에 표시.
// G키: 디버그로 도끼 지급(벌목 테스트용).
public class DebugHud : MonoBehaviour
{
    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private PlayerTools tools;
    [SerializeField] private Inventory inventory;
    [SerializeField] private PlayerStats stats; // 선택

    private GUIStyle style;

    private void Start()
    {
        if (interactor == null) interactor = FindObjectOfType<PlayerInteractor>();
        if (tools == null)      tools      = FindObjectOfType<PlayerTools>();
        if (inventory == null)  inventory  = FindObjectOfType<Inventory>();
        if (stats == null)      stats      = FindObjectOfType<PlayerStats>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G) && tools != null)
            tools.GiveAxe();
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

        if (tools != null)
            sb.AppendLine($"도끼: {(tools.HasAxe ? "있음" : "없음")}   (G: 디버그 지급)");

        if (inventory != null)
        {
            sb.AppendLine($"나뭇가지: {inventory.Get(ResourceKind.Stick)}");
            sb.AppendLine($"장작: {inventory.Get(ResourceKind.Firewood)}");
            sb.AppendLine($"식량: {inventory.Get(ResourceKind.Food)}");
            sb.AppendLine($"골드: {inventory.Gold}");
        }

        if (stats != null)
            sb.AppendLine($"온기: {stats.Warmth:0}   허기: {stats.Hunger:0}   {(stats.IsDown ? "[쓰러짐]" : "")}");

        GUI.Box(new Rect(10, 10, 260, 200), GUIContent.none);
        GUI.Label(new Rect(20, 16, 240, 190), sb.ToString(), style);
    }
}
