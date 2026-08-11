using System.Collections.Generic;
using System.Text;
using UnityEngine;

// 제작 목록. 열기 = 제작대에서 E, 또는 어디서나 열기 키(기본 C).
// 시설에서 열면 그 시설 레시피 + 맨손 레시피만 보인다. 키로 열면 전체.
// 위/아래 선택, E 제작(1회, 목록 유지), Q/ESC 닫기.
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
    private CraftStation? filter;   // null이면 전체 표시
    private float craftProgress;    // 제작 진행 시간

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

    // 제작대 등에서 호출 — 해당 시설 레시피만 보여준다
    public void Open(CraftStation? stationFilter = null)
    {
        filter = stationFilter;
        cursor = 0;
        SetOpen(true);
    }

    public void Close() => SetOpen(false);

    private void SetOpen(bool open)
    {
        if (IsOpen == open) return;
        IsOpen = open;
        if (open) { UIInputLock.Push(); skipFirstInput = true; }
        else { UIInputLock.Release(); filter = null; }
    }

    private void Update()
    {
        if (Input.GetKeyDown(openKey))
        {
            if (IsOpen) SetOpen(false);
            else Open(null); // 키로 열면 전체 목록
        }
        if (!IsOpen) return;

        RefreshList();

        if (skipFirstInput) { skipFirstInput = false; return; }

        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Escape)) { SetOpen(false); return; }
        if (shown.Count == 0) return;

        if (Input.GetKeyDown(KeyCode.DownArrow)) { cursor = (cursor + 1) % shown.Count; craftProgress = 0f; }
        if (Input.GetKeyDown(KeyCode.UpArrow)) { cursor = (cursor - 1 + shown.Count) % shown.Count; craftProgress = 0f; }

        if (crafting == null) return;

        CraftingRecipe recipe = shown[cursor];

        // 제작 시간이 있으면 E를 누르고 있는 동안 진행
        if (recipe.craftSeconds > 0f)
        {
            if (Input.GetKey(KeyCode.E) && crafting.BlockReason(recipe) == null)
            {
                craftProgress += Time.deltaTime;
                if (craftProgress >= recipe.craftSeconds)
                {
                    crafting.TryCraft(recipe);
                    craftProgress = 0f;
                }
            }
            else craftProgress = 0f;
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            crafting.TryCraft(recipe); // 목록 유지 — 연달아 제작 가능
        }
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
            if (filter.HasValue &&
                r.requiredStation != filter.Value &&
                r.requiredStation != CraftStation.Hand) continue;
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
        string title = filter.HasValue ? $"제작 — {CraftingStation.Label(filter.Value)}" : "제작";
        GUI.Label(new Rect(px + 14f, py + 8f, w - 28f, 22f), title, headStyle);

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
                ? PlayerCrafting.ItemLabel(r.output)
                : r.outputName);
            if (r.outputAmount > 1) sb.Append($" x{r.outputAmount}");

            if (r.requiredStation != CraftStation.Hand)
                sb.Append($"  [{CraftingStation.Label(r.requiredStation)}]");

            sb.Append("   ←  ");
            if (r.costs != null)
            {
                for (int c = 0; c < r.costs.Length; c++)
                {
                    int have = inventory != null ? inventory.Get(r.costs[c].item) : 0;
                    sb.Append($"{PlayerCrafting.ItemLabel(r.costs[c].item)} {have}/{r.costs[c].amount}   ");
                }
            }

            string reason = crafting != null ? crafting.BlockReason(r) : "제작 불가";
            if (reason != null) sb.Append($"({reason})");
            else if (r.craftSeconds > 0f)
                sb.Append(i == cursor && craftProgress > 0f
                    ? $"[{craftProgress:0.0}/{r.craftSeconds:0.0}초]"
                    : $"[{r.craftSeconds:0.#}초]");

            Rect rect = new Rect(px + 14f, py + 38f + i * (rowH + 4f), w - 28f, rowH);
            Color prev = GUI.color;
            if (i == cursor) GUI.color = reason == null ? new Color(1f, 0.95f, 0.6f) : new Color(1f, 0.75f, 0.65f);
            else if (reason != null) GUI.color = new Color(0.72f, 0.72f, 0.72f);
            GUI.Box(rect, sb.ToString(), rowStyle);
            GUI.color = prev;
        }

        GUI.Label(new Rect(px + 14f, py + panelH - 26f, w - 28f, 22f),
            "위/아래 선택 · E 제작(시간 있는 것은 누르고 있기) · Q/ESC 닫기", headStyle);
    }
}