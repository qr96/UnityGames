using System.Collections.Generic;
using UnityEngine;

// 화로 메뉴. HearthInteractable이 E로 연다.
// 항목: 불 피우기(꺼졌을 때) / 연료 넣기 / 강화(미강화일 때만).
// 위/아래 선택, E 확정, Q/ESC 닫기.
public class HearthMenuUI : MonoBehaviour
{
    private enum Entry { Relight, Refuel, Upgrade }

    [SerializeField] private FuelSelectUI fuelSelect; // 비우면 씬에서 찾음

    public bool IsOpen { get; private set; }

    private readonly List<Entry> entries = new List<Entry>();
    private Hearth hearth;
    private Inventory inventory;
    private int cursor;
    private bool skipFirstInput;

    private GUIStyle rowStyle;
    private GUIStyle headStyle;

    private void Start()
    {
        if (fuelSelect == null) fuelSelect = FindObjectOfType<FuelSelectUI>();
    }

    private void OnDisable()
    {
        if (IsOpen) { IsOpen = false; UIInputLock.Release(); }
    }

    public void Open(Hearth target, Inventory inv)
    {
        if (IsOpen || target == null) return;

        hearth = target;
        inventory = inv;
        BuildEntries();
        if (entries.Count == 0) return;

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

    private void BuildEntries()
    {
        entries.Clear();
        if (hearth == null) return;

        if (!hearth.IsLit) entries.Add(Entry.Relight);
        else entries.Add(Entry.Refuel);

        if (hearth.CanUpgrade) entries.Add(Entry.Upgrade);
    }

    private void Update()
    {
        if (!IsOpen) return;
        if (skipFirstInput) { skipFirstInput = false; return; }

        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }

        BuildEntries();
        if (entries.Count == 0) { Close(); return; }
        if (cursor >= entries.Count) cursor = entries.Count - 1;

        if (Input.GetKeyDown(KeyCode.DownArrow)) cursor = (cursor + 1) % entries.Count;
        if (Input.GetKeyDown(KeyCode.UpArrow))   cursor = (cursor - 1 + entries.Count) % entries.Count;

        if (Input.GetKeyDown(KeyCode.E)) Confirm();
    }

    private void Confirm()
    {
        Entry e = entries[cursor];
        Hearth h = hearth;   // Close 후에도 쓰기 위해 보관
        Inventory inv = inventory;

        switch (e)
        {
            case Entry.Relight:
            case Entry.Refuel:
                if (fuelSelect == null) fuelSelect = FindObjectOfType<FuelSelectUI>();
                if (fuelSelect == null) { Debug.LogWarning("[화로] FuelSelectUI가 씬에 없음"); return; }
                Close();
                fuelSelect.Open(h, inv, e == Entry.Relight);
                break;

            case Entry.Upgrade:
                if (h.TryUpgrade(inv))
                {
                    Debug.Log($"[화로] 강화 완료 — 연료 소모 감소");
                    Close();
                }
                else Debug.Log($"[화로] 강화 실패 — 돌 {h.UpgradeStoneCost}개 필요");
                break;
        }
    }

    private void OnGUI()
    {
        if (!IsOpen || hearth == null) return;

        if (rowStyle == null)
        {
            rowStyle = new GUIStyle(GUI.skin.box) { fontSize = 14, alignment = TextAnchor.MiddleLeft };
            headStyle = new GUIStyle(GUI.skin.label) { fontSize = 15 };
            headStyle.normal.textColor = Color.white;
        }

        const float w = 380f, rowH = 30f;
        float panelH = 78f + entries.Count * (rowH + 4f);
        float px = (Screen.width - w) * 0.5f;
        float py = (Screen.height - panelH) * 0.5f;

        GUI.Box(new Rect(px, py, w, panelH), GUIContent.none);
        GUI.Label(new Rect(px + 14f, py + 8f, w - 28f, 22f),
            $"{hearth.DisplayName}  (연료 {Mathf.FloorToInt(hearth.Fuel)}/{Mathf.FloorToInt(hearth.FuelCapacity)})",
            headStyle);

        for (int i = 0; i < entries.Count; i++)
        {
            string text = entries[i] switch
            {
                Entry.Relight => "불 피우기",
                Entry.Refuel  => "연료 넣기",
                Entry.Upgrade => $"강화 — 돌 {(inventory != null ? inventory.Get(ResourceKind.Stone) : 0)}/{hearth.UpgradeStoneCost}"
                                 + " (연료 소모 감소, 1회)",
                _ => "?",
            };

            bool ok = entries[i] != Entry.Upgrade ||
                      (inventory != null && inventory.Has(ResourceKind.Stone, hearth.UpgradeStoneCost));

            Rect r = new Rect(px + 14f, py + 36f + i * (rowH + 4f), w - 28f, rowH);
            Color prev = GUI.color;
            if (i == cursor) GUI.color = ok ? new Color(1f, 0.95f, 0.6f) : new Color(1f, 0.75f, 0.65f);
            else if (!ok) GUI.color = new Color(0.75f, 0.75f, 0.75f);
            GUI.Box(r, text, rowStyle);
            GUI.color = prev;
        }

        GUI.Label(new Rect(px + 14f, py + panelH - 26f, w - 28f, 22f),
            "위/아래 선택 · E 확정 · Q/ESC 닫기", headStyle);
    }
}
