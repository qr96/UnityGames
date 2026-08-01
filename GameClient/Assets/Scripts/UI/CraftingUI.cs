using System.Text;
using UnityEngine;

// 제작 목록. CraftingStation이 Open()으로 띄운다.
// 위/아래 선택, E 제작(1회, 목록 유지), Q/ESC 닫기. 열려 있는 동안 UIInputLock으로 조작 잠금.
public class CraftingUI : MonoBehaviour
{
    private CraftingStation station;
    private Inventory inventory;
    private int cursor;
    private bool skipFirstInput;

    public bool IsOpen { get; private set; }

    private GUIStyle rowStyle;
    private GUIStyle headStyle;

    public void Open(CraftingStation targetStation, Inventory inv)
    {
        if (IsOpen) return;
        if (targetStation == null || targetStation.Recipes == null || targetStation.Recipes.Length == 0)
        {
            Debug.Log("[제작대] 레시피 없음");
            return;
        }

        station = targetStation;
        inventory = inv;
        cursor = 0;
        skipFirstInput = true;
        IsOpen = true;
        UIInputLock.Push();
    }

    private void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        UIInputLock.Release();
        station = null;
    }

    private void OnDisable()
    {
        if (IsOpen) { IsOpen = false; UIInputLock.Release(); }
    }

    private void Update()
    {
        if (!IsOpen) return;
        if (skipFirstInput) { skipFirstInput = false; return; }

        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }

        int n = station.Recipes.Length;
        if (Input.GetKeyDown(KeyCode.DownArrow)) cursor = (cursor + 1) % n;
        if (Input.GetKeyDown(KeyCode.UpArrow))   cursor = (cursor - 1 + n) % n;

        if (Input.GetKeyDown(KeyCode.E))
            station.TryCraft(station.Recipes[cursor]); // 목록 유지 — 연달아 제작 가능
    }

    private void OnGUI()
    {
        if (!IsOpen || station == null) return;

        if (rowStyle == null)
        {
            rowStyle = new GUIStyle(GUI.skin.box) { fontSize = 14, alignment = TextAnchor.MiddleLeft };
            headStyle = new GUIStyle(GUI.skin.label) { fontSize = 15 };
            headStyle.normal.textColor = Color.white;
        }

        CraftingRecipe[] list = station.Recipes;
        const float w = 460f, rowH = 32f;
        float panelH = 76f + list.Length * (rowH + 4f);
        float px = (Screen.width - w) * 0.5f;
        float py = (Screen.height - panelH) * 0.5f;

        GUI.Box(new Rect(px, py, w, panelH), GUIContent.none);
        GUI.Label(new Rect(px + 14f, py + 8f, w - 28f, 22f), $"{station.StationName} — 제작", headStyle);

        for (int i = 0; i < list.Length; i++)
        {
            CraftingRecipe r = list[i];
            var sb = new StringBuilder();
            sb.Append(string.IsNullOrEmpty(r.outputName) ? CraftingStation.KindLabel(r.outputKind) : r.outputName);
            if (r.outputAmount > 1) sb.Append($" x{r.outputAmount}");
            sb.Append("   ←  ");

            if (r.costs != null)
            {
                for (int c = 0; c < r.costs.Length; c++)
                {
                    int have = inventory != null ? inventory.Get(r.costs[c].kind) : 0;
                    sb.Append($"{CraftingStation.KindLabel(r.costs[c].kind)} {have}/{r.costs[c].amount}   ");
                }
            }

            bool ok = station.HasMaterials(r) && station.HasRoom(r);
            if (!ok) sb.Append(station.HasMaterials(r) ? "(칸 없음)" : "(재료 부족)");

            Rect rect = new Rect(px + 14f, py + 36f + i * (rowH + 4f), w - 28f, rowH);
            Color prev = GUI.color;
            if (i == cursor) GUI.color = ok ? new Color(1f, 0.95f, 0.6f) : new Color(1f, 0.7f, 0.6f);
            else if (!ok)    GUI.color = new Color(0.75f, 0.75f, 0.75f);
            GUI.Box(rect, sb.ToString(), rowStyle);
            GUI.color = prev;
        }

        GUI.Label(new Rect(px + 14f, py + panelH - 26f, w - 28f, 22f),
            "위/아래 선택 · E 제작 · Q/ESC 닫기", headStyle);
    }
}
