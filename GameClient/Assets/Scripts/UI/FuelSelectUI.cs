using System.Collections.Generic;
using UnityEngine;

// 화로 연료 선택 목록. HearthInteractable이 Open()으로 띄운다.
// 방향키 위/아래 선택, E로 1개씩 투입(연달아 가능), Q/ESC 닫기.
// 열려 있는 동안 UIInputLock으로 조작 잠금.
public class FuelSelectUI : MonoBehaviour
{
    private readonly List<ItemDef> candidates = new List<ItemDef>();
    private Hearth hearth;
    private Inventory inventory;
    private bool relightMode;
    private int cursor;
    private bool skipFirstInput; // 여는 프레임의 E가 그대로 확정되지 않게

    public bool IsOpen { get; private set; }

    private GUIStyle rowStyle;
    private GUIStyle headStyle;

    public void Open(Hearth targetHearth, Inventory inv, bool relight)
    {
        if (IsOpen) return;

        hearth = targetHearth;
        inventory = inv;
        relightMode = relight;

        candidates.Clear();
        if (inventory != null)
        {
            for (int i = 0; i < inventory.SlotCount; i++)
            {
                Inventory.Slot s = inventory.Slots[i];
                if (s.IsEmpty || !s.def.IsFuel) continue;
                if (!candidates.Contains(s.def)) candidates.Add(s.def);
            }
        }

        if (candidates.Count == 0)
        {
            Debug.Log("[화로] 넣을 수 있는 연료 없음");
            return;
        }

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
        hearth = null;
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

        int n = candidates.Count;
        if (Input.GetKeyDown(KeyCode.DownArrow)) cursor = (cursor + 1) % n;
        if (Input.GetKeyDown(KeyCode.UpArrow)) cursor = (cursor - 1 + n) % n;

        if (Input.GetKeyDown(KeyCode.E)) Confirm();
    }

    private void Confirm()
    {
        if (hearth == null || inventory == null) { Close(); return; }

        ItemDef def = candidates[cursor];

        if (relightMode)
        {
            if (hearth.RelightWith(inventory, def)) Close();       // 점화되면 닫음
            else Debug.Log("[화로] 점화 실패 — 연료 부족");
            return;
        }

        // 1개 투입. 목록은 열린 채로 두어 연달아 넣을 수 있게 함.
        if (!hearth.AddFuelUnit(inventory, def))
            Debug.Log("[화로] 넣지 못함 — 용량 여유 또는 보유량 부족");
    }

    private void OnGUI()
    {
        if (!IsOpen) return;

        if (rowStyle == null)
        {
            rowStyle = new GUIStyle(GUI.skin.box) { fontSize = 14, alignment = TextAnchor.MiddleLeft };
            headStyle = new GUIStyle(GUI.skin.label) { fontSize = 15 };
            headStyle.normal.textColor = Color.white;
        }

        const float w = 360f, rowH = 30f;
        float panelH = 76f + candidates.Count * (rowH + 4f);
        float px = (Screen.width - w) * 0.5f;
        float py = (Screen.height - panelH) * 0.5f;

        GUI.Box(new Rect(px, py, w, panelH), GUIContent.none);
        GUI.Label(new Rect(px + 14f, py + 8f, w - 28f, 22f),
            relightMode ? "불 피우기 — 연료 선택" : "장작 넣기 — 연료 선택", headStyle);

        for (int i = 0; i < candidates.Count; i++)
        {
            ItemDef def = candidates[i];
            int have = inventory != null ? inventory.Get(def) : 0;
            int room = hearth != null ? hearth.RoomForUnits(def) : 0;
            string text = $"{def.displayName}   보유 {have}   연료 {def.fuelValue:0.##}/개   더 넣을 수 있음 {Mathf.Min(have, room)}개";

            Rect r = new Rect(px + 14f, py + 36f + i * (rowH + 4f), w - 28f, rowH);
            Color prev = GUI.color;
            if (i == cursor) GUI.color = new Color(1f, 0.95f, 0.6f);
            GUI.Box(r, text, rowStyle);
            GUI.color = prev;
        }

        GUI.Label(new Rect(px + 14f, py + panelH - 26f, w - 28f, 22f),
            (relightMode ? "위/아래 선택 · E 점화 · Q/ESC 취소" : "위/아래 선택 · E 1개 투입 · Q/ESC 닫기"), headStyle);
    }
}