using System.Text;
using UnityEngine;

// 화면 카운터. 칸 사용량 / 무게 / 골드 표시. 초과 시 색으로 강조.
// 임시 그래픽(OnGUI) — 폴리싱 단계에서 교체.
public class InventoryCounter : MonoBehaviour
{
    [SerializeField] private Inventory inventory; // 비우면 씬에서 찾음
    [SerializeField] private Vector2 screenMargin = new Vector2(16f, 16f);

    private GUIStyle style;
    private string cached = "";

    private void Start()
    {
        if (inventory == null) inventory = FindObjectOfType<Inventory>();
        if (inventory != null)
        {
            inventory.OnChanged += Rebuild;
            Rebuild();
        }
    }

    private void OnDestroy()
    {
        if (inventory != null) inventory.OnChanged -= Rebuild;
    }

    private void Rebuild()
    {
        int used = 0;
        for (int i = 0; i < inventory.SlotCount; i++)
            if (!inventory.Slots[i].IsEmpty) used++;

        var sb = new StringBuilder();
        sb.AppendLine($"칸 {used}/{inventory.SlotCount}");
        sb.AppendLine($"무게 {inventory.CurrentWeight:0.#}/{inventory.WeightLimit:0.#}" +
                      (inventory.IsOverweight ? $"  — 초과 (속도 x{inventory.SpeedMultiplier:0.00})" : ""));
        sb.Append($"골드 {inventory.Gold}");
        cached = sb.ToString();
    }

    private void OnGUI()
    {
        if (inventory == null) return;

        if (style == null)
            style = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.UpperRight };

        style.normal.textColor = inventory.IsOverweight ? new Color(1f, 0.5f, 0.4f) : Color.white;

        float w = 340f, h = 72f;
        Rect r = new Rect(Screen.width - w - screenMargin.x, screenMargin.y, w, h);
        GUI.Box(r, GUIContent.none);
        GUI.Label(new Rect(r.x, r.y + 4f, r.width - 8f, r.height), cached, style);
    }
}
