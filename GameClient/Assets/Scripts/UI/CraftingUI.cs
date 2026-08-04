using System.Collections.Generic;
using System.Text;
using UnityEngine;

// 제작 목록. 열기 키(기본 C), 위/아래 선택, E 제작(1회, 목록 유지), Q/ESC 닫기.
// 맨손 레시피는 항상 보이고, 시설 레시피는 잠금 사유가 함께 표시된다.
// (v0.7의 Tab 통합은 탭 전환 키가 정해지면 반영)
public class CraftingUI : MonoBehaviour
{
    [SerializeField] private PlayerCrafting crafting; // 비우면 씬에서 찾음
    [SerializeField] private Inventory inventory;     // 비우면 씬에서 찾음
    [SerializeField] private KeyCode openKey = KeyCode.C;
    [Tooltip("끄면 제작 불가 레시피를 목록에서 숨김")]
    [SerializeField] private bool showLocked = true;

    public bool IsOpen { get; private set; }

    private readonly List<CraftingRecipe> shown = new List<CraftingRecipe>();
    private int cursor;
    private bool skipFirstInput;

    private GUIStyle rowStyle;
    private GUIStyle headStyle;

    private void Start()
    {
        if (crafting == null) crafting = FindObjectOfType<PlayerCrafting>();
        if (inventory == null) inventory = FindObjectOfType<Inventory>();
    }

    private void OnDisable()
    {
        if (IsOpen) { IsOpen = false; UIInputLock.Release(); }
    }

    private void SetOpen(bool open)
    {
        if (IsOpen == open) return;
        IsOpen = open;
        if (open) { UIInputLock.Push(); skipFirstInput = true; }
        else UIInputLock.Release();
    }

    private void Update()
    {
        if (Input.GetKeyDown(openKey)) SetOpen(!IsOpen);
        if (!IsOpen) return;

        RefreshList();

        if (skipFirstInput) { skipFirstInput = false; return; }

        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Escape)) { SetOpen(false); return; }
        if (shown.Count == 0) return;

        if (Input.GetKeyDown(KeyCode.DownArrow)) cursor = (cursor + 1) % shown.Count;
        if (Input.GetKeyDown(KeyCode.UpArrow)) cursor = (cursor - 1 + shown.Count) % shown.Count;

        if (Input.GetKeyDown(KeyCode.E) && crafting != null)
            crafting.TryCraft(shown[cursor]); // 목록 유지 — 연달아 제작 가능
    }

    private void RefreshList()
    {
        shown.Clear();
        if (crafting == null || crafting.Book == null || crafting.Book.recipes == null) return;

        CraftingRecipe[] all = crafting.Book.recipes;
        for (int i = 0; i < all.Length; i++)
        {
            CraftingRecipe r = all[i];
            if (r == null) continue;
            if (!showLocked && crafting.BlockReason(r) != null) continue;
            shown.Add(r);
        }

        if (cursor >= shown.Count) cursor = Mathf.Max(0, shown.Count - 1);
    }

    private void OnGUI()
    {
        if (!IsOpen) return;

        if (rowStyle == null)
        {
            rowStyle = new GUIStyle(GUI.skin.box) { fontSize = 13, alignment = TextAnchor.MiddleLeft };
            headStyle = new GUIStyle(GUI.skin.label) { fontSize = 15 };
            headStyle.normal.textColor = Color.white;
        }

        const float w = 500f, rowH = 30f;
        int n = Mathf.Max(1, shown.Count);
        float panelH = 78f + n * (rowH + 4f);
        float px = (Screen.width - w) * 0.5f;
        float py = (Screen.height - panelH) * 0.5f;

        GUI.Box(new Rect(px, py, w, panelH), GUIContent.none);
        GUI.Label(new Rect(px + 14f, py + 8f, w - 28f, 22f), "제작", headStyle);

        if (shown.Count == 0)
        {
            GUI.Label(new Rect(px + 14f, py + 40f, w - 28f, 22f),
                "레시피 없음 (Recipe Book 확인)", headStyle);
        }

        for (int i = 0; i < shown.Count; i++)
        {
            CraftingRecipe r = shown[i];
            var sb = new StringBuilder();

            sb.Append(string.IsNullOrEmpty(r.outputName)
                ? PlayerCrafting.KindLabel(r.outputKind)
                : r.outputName);
            if (r.outputAmount > 1) sb.Append($" x{r.outputAmount}");

            if (r.requiredStation != CraftStation.Hand)
                sb.Append($"  [{CraftingStation.Label(r.requiredStation)}]");

            sb.Append("   ←  ");
            if (r.costs != null)
            {
                for (int c = 0; c < r.costs.Length; c++)
                {
                    int have = inventory != null ? inventory.Get(r.costs[c].kind) : 0;
                    sb.Append($"{PlayerCrafting.KindLabel(r.costs[c].kind)} {have}/{r.costs[c].amount}   ");
                }
            }

            string reason = crafting != null ? crafting.BlockReason(r) : "제작 불가";
            if (reason != null) sb.Append($"({reason})");

            Rect rect = new Rect(px + 14f, py + 38f + i * (rowH + 4f), w - 28f, rowH);
            Color prev = GUI.color;
            if (i == cursor) GUI.color = reason == null ? new Color(1f, 0.95f, 0.6f) : new Color(1f, 0.75f, 0.65f);
            else if (reason != null) GUI.color = new Color(0.72f, 0.72f, 0.72f);
            GUI.Box(rect, sb.ToString(), rowStyle);
            GUI.color = prev;
        }

        GUI.Label(new Rect(px + 14f, py + panelH - 26f, w - 28f, 22f),
            "위/아래 선택 · E 제작 · Q/ESC 닫기", headStyle);
    }
}