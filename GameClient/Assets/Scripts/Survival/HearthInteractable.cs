using UnityEngine;

// 화로 상호작용. E → 메뉴(HearthMenuUI): 불 피우기 / 연료 넣기 / 강화.
// 화로 오브젝트에 Hearth와 함께 붙인다.
public class HearthInteractable : InteractableBase
{
    [SerializeField] private Hearth hearth;         // 같은 오브젝트면 자동
    [SerializeField] private Inventory inventory;   // 비우면 씬에서 찾음
    [SerializeField] private HearthMenuUI menu;     // 비우면 씬에서 찾음

    private void Awake()
    {
        if (hearth == null) hearth = GetComponent<Hearth>();
    }

    private void Start()
    {
        if (inventory == null) inventory = FindObjectOfType<Inventory>();
        if (menu == null) menu = FindObjectOfType<HearthMenuUI>();
    }

    public override string Prompt
    {
        get
        {
            if (hearth == null) return "화로";
            if (!hearth.IsLit) return $"{hearth.DisplayName} (꺼짐)";
            return $"{hearth.DisplayName} (연료 {Mathf.FloorToInt(hearth.Fuel)}/{Mathf.FloorToInt(hearth.FuelCapacity)})";
        }
    }

    public override bool CanInteract(GameObject interactor) => hearth != null;

    public override void Interact(GameObject interactor)
    {
        if (menu == null) menu = FindObjectOfType<HearthMenuUI>();
        if (menu == null)
        {
            Debug.LogWarning("[화로] HearthMenuUI가 씬에 없음");
            return;
        }
        if (inventory == null) inventory = FindObjectOfType<Inventory>();

        menu.Open(hearth, inventory);
    }

    // 남은 지속 시간 표기 (분:초) — HearthGauge 옵션 표기에서 사용
    public static string FormatTime(float seconds)
    {
        if (float.IsInfinity(seconds)) return "∞";
        int t = Mathf.Max(0, Mathf.FloorToInt(seconds));
        return $"{t / 60}:{t % 60:00}";
    }
}