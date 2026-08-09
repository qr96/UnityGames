using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 상시 HUD를 uGUI 캔버스 하나로 그린다. IMGUI(OnGUI)를 대체 — 드로우콜이 캔버스 단위로 묶인다.
// 로직은 기존 컴포넌트(PlayerStats·Hotbar·QuickFood·Inventory 등)에 그대로 있고, 여기서는 표시만 한다.
public class HudUI : MonoBehaviour
{
    [Header("참조 (비우면 씬에서 찾음)")]
    [SerializeField] private PlayerStats stats;
    [SerializeField] private Inventory inventory;
    [SerializeField] private Hotbar hotbar;
    [SerializeField] private QuickFood quickFood;
    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private PickupCollector collector;
    [SerializeField] private Camera worldCamera;

    [Header("캔버스")]
    [Tooltip("씬의 Canvas를 연결. 크기·해상도 설정은 그 Canvas Scaler에서 한다")]
    [SerializeField] private Canvas targetCanvas;

    [Header("화로 게이지")]
    [Tooltip("화로 머리 위 높이(월드)")]
    [SerializeField] private float gaugeWorldHeight = 2.2f;
    [Tooltip("그 화로가 지금 상호작용 대상이면 게이지를 숨김(라벨과 겹치지 않게)")]
    [SerializeField] private bool hideGaugeWhenTargeted = true;

    [Header("가독성")]
    [Tooltip("라벨·카운터 뒤에 깔 반투명 배경 색")]
    [SerializeField] private Color labelBackColor = new Color(0f, 0f, 0f, 0.55f);

    [Header("글자 크기")]
    [SerializeField] private int statFontSize = 12;
    [SerializeField] private int slotFontSize = 12;
    [SerializeField] private int counterFontSize = 16;
    [SerializeField] private int worldLabelFontSize = 15;

    [Header("스태미나 원형 게이지")]
    [Tooltip("캐릭터를 따라다닌다. 비우면 PlayerMovement로 찾음")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private float gaugeSize = 56f;
    [Tooltip("캐릭터 기준 화면 오프셋(픽셀)")]
    [SerializeField] private Vector2 gaugeScreenOffset = new Vector2(46f, -18f);
    [SerializeField] private float gaugeThickness = 0.62f;   // 안쪽 반지름 비율(클수록 얇음)
    [SerializeField] private Color gaugeBorderColor = new Color(0f, 0f, 0f, 0.85f);
    [SerializeField] private Color gaugeBackColor = new Color(0f, 0f, 0f, 0.45f);
    [Tooltip("가득 찼을 때 숨김")]
    [SerializeField] private bool hideWhenFull = true;

    [Header("스탯 막대")]
    [SerializeField] private Vector2 barSize = new Vector2(220f, 16f);
    [SerializeField] private float barGap = 6f;
    [SerializeField] private Color healthColor = new Color(0.9f, 0.35f, 0.4f);
    [SerializeField] private Color warmthColor = new Color(1f, 0.6f, 0.2f);
    [SerializeField] private Color hungerColor = new Color(0.6f, 0.8f, 0.35f);
    [SerializeField] private Color staminaColor = new Color(0.4f, 0.75f, 1f);
    [SerializeField] private Color lowColor = new Color(1f, 0.35f, 0.3f);
    [Range(0f, 1f)][SerializeField] private float lowThreshold = 0.25f;

    private Canvas canvas;
    private RectTransform root;     // 모든 요소가 담기는 부모

    private class Bar
    {
        public Image fill;
        public Text label;
        public RectTransform fillRect;
        public float width;
    }

    private Bar healthBar, warmthBar, hungerBar;

    private RectTransform staminaRoot;
    private Image staminaFill;

    private class Slot
    {
        public Image background;
        public Text label;
    }

    private readonly List<Slot> hotbarSlots = new List<Slot>();
    private Slot foodSlot;

    private Text counterText;
    private RectTransform counterBackRect;
    private Text targetLabel;
    private RectTransform targetLabelRect;
    private Text pickupPrompt;
    private RectTransform pickupBackRect;

    private class Gauge
    {
        public RectTransform root;
        public Image back;
        public Image fill;
        public RectTransform fillRect;
        public Text label;
    }

    private readonly Dictionary<Hearth, Gauge> gauges = new Dictionary<Hearth, Gauge>();
    private RectTransform gaugeParent;

    private const float GaugeWidth = 96f;
    private const float GaugeHeight = 10f;

    private void Start()
    {
        if (stats == null) stats = FindObjectOfType<PlayerStats>();
        if (inventory == null) inventory = FindObjectOfType<Inventory>();
        if (hotbar == null) hotbar = FindObjectOfType<Hotbar>();
        if (quickFood == null) quickFood = FindObjectOfType<QuickFood>();
        if (interactor == null) interactor = FindObjectOfType<PlayerInteractor>();
        if (collector == null) collector = FindObjectOfType<PickupCollector>();
        if (worldCamera == null) worldCamera = Camera.main;

        Build();
    }

    private void Build()
    {
        canvas = targetCanvas;
        if (canvas == null)
        {
            Debug.LogError("[HUD] Target Canvas가 연결되지 않음 — 씬에 Canvas를 만들어 연결할 것 " +
                           "(GameObject > UI > Canvas, Canvas Scaler = Scale With Screen Size)");
            enabled = false;
            return;
        }

        // 모든 요소를 담을 루트 — 여기에 배율을 건다
        root = UIKit.CreateRect("Root", canvas.transform);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        // ── 스탯 막대 (좌하단) ──
        float x = 16f;
        float y = 16f + (barSize.y + barGap) * 2f;

        healthBar = CreateBar("Health", new Vector2(x, y), healthColor);
        warmthBar = CreateBar("Warmth", new Vector2(x, y - (barSize.y + barGap)), warmthColor);
        hungerBar = CreateBar("Hunger", new Vector2(x, y - (barSize.y + barGap) * 2f), hungerColor);

        BuildStaminaGauge();

        // ── 핫바 (하단 중앙) ──
        const float slotW = 68f, slotH = 46f, slotGap = 4f;
        float total = Hotbar.SlotCount * slotW + (Hotbar.SlotCount - 1) * slotGap;

        for (int i = 0; i < Hotbar.SlotCount; i++)
        {
            float sx = -total * 0.5f + i * (slotW + slotGap) + slotW * 0.5f;
            hotbarSlots.Add(CreateSlot($"Hotbar{i}", new Vector2(sx, 16f + slotH * 0.5f),
                                       new Vector2(slotW, slotH), new Vector2(0.5f, 0f)));
        }

        foodSlot = CreateSlot("QuickFood",
            new Vector2(total * 0.5f + 12f + 46f, 16f + slotH * 0.5f),
            new Vector2(92f, slotH), new Vector2(0.5f, 0f));

        // ── 인벤 카운터 (우상단) ──
        Image counterBack = UIKit.CreateImage("CounterBack", root, labelBackColor);
        UIKit.SetRect(counterBack.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                      new Vector2(-12f, -12f), new Vector2(240f, 56f));

        counterBackRect = counterBack.rectTransform;
        counterText = UIKit.CreateText("Counter", counterBack.transform, counterFontSize,
                                       TextAnchor.MiddleRight);
        UIKit.SetRect(counterText.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                      new Vector2(-10f, 0f), new Vector2(220f, 52f));

        // ── 타겟 라벨 (월드 추적) — 반투명 배경판 위에 ──
        Image targetBack = UIKit.CreateImage("TargetLabelBack", root, labelBackColor);
        targetLabelRect = targetBack.rectTransform;
        UIKit.SetRect(targetLabelRect, new Vector2(0f, 0f), new Vector2(0.5f, 0f),
                      Vector2.zero, new Vector2(300f, 26f));

        targetLabel = UIKit.CreateText("TargetLabel", targetBack.transform,
                                       worldLabelFontSize, TextAnchor.MiddleCenter);
        UIKit.SetRect(targetLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                      Vector2.zero, new Vector2(300f, 26f));
        targetBack.gameObject.SetActive(false);

        // ── 줍기 안내 ──
        Image pickupBack = UIKit.CreateImage("PickupPromptBack", root, labelBackColor);
        UIKit.SetRect(pickupBack.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                      new Vector2(0f, 96f), new Vector2(300f, 26f));

        pickupBackRect = pickupBack.rectTransform;
        pickupPrompt = UIKit.CreateText("PickupPrompt", pickupBack.transform,
                                        worldLabelFontSize, TextAnchor.MiddleCenter);
        UIKit.SetRect(pickupPrompt.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                      Vector2.zero, new Vector2(300f, 26f));
        pickupBack.gameObject.SetActive(false);

        // 화로 게이지 부모
        gaugeParent = UIKit.CreateRect("HearthGauges", root);
        UIKit.SetRect(gaugeParent, new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero, Vector2.zero);
    }

    private void BuildStaminaGauge()
    {
        Sprite ring = UIKit.CreateRingSprite(128, gaugeThickness);
        Sprite border = UIKit.CreateRingSprite(128, gaugeThickness - 0.08f);

        staminaRoot = UIKit.CreateRect("StaminaGauge", root);
        UIKit.SetRect(staminaRoot, new Vector2(0f, 0f), new Vector2(0.5f, 0.5f),
                      Vector2.zero, new Vector2(gaugeSize, gaugeSize));

        // 테두리(살짝 크게) → 배경 → 채움 순서
        Image borderImg = UIKit.CreateImage("Border", staminaRoot, gaugeBorderColor);
        borderImg.sprite = border;
        UIKit.SetRect(borderImg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                      Vector2.zero, new Vector2(gaugeSize + 6f, gaugeSize + 6f));

        Image backImg = UIKit.CreateImage("Back", staminaRoot, gaugeBackColor);
        backImg.sprite = ring;
        UIKit.SetRect(backImg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                      Vector2.zero, new Vector2(gaugeSize, gaugeSize));

        staminaFill = UIKit.CreateImage("Fill", staminaRoot, staminaColor);
        staminaFill.sprite = ring;
        staminaFill.type = Image.Type.Filled;
        staminaFill.fillMethod = Image.FillMethod.Radial360;
        staminaFill.fillOrigin = (int)Image.Origin360.Top;
        staminaFill.fillClockwise = true;
        UIKit.SetRect(staminaFill.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                      Vector2.zero, new Vector2(gaugeSize, gaugeSize));
    }

    private void UpdateStaminaGauge()
    {
        if (staminaRoot == null || stats == null) return;

        if (followTarget == null)
        {
            PlayerMovement pm = FindObjectOfType<PlayerMovement>();
            if (pm != null) followTarget = pm.transform;
        }

        float ratio = Mathf.Clamp01(stats.StaminaNormalized);
        bool show = followTarget != null && worldCamera != null
                    && (!hideWhenFull || ratio < 0.999f || stats.IsExhausted);

        if (show)
        {
            Vector3 sp = worldCamera.WorldToScreenPoint(followTarget.position + Vector3.up * 1f);
            show = sp.z > 0f;
            if (show) staminaRoot.anchoredPosition = ScreenToCanvas(sp) + gaugeScreenOffset;
        }

        if (staminaRoot.gameObject.activeSelf != show) staminaRoot.gameObject.SetActive(show);
        if (!show) return;

        staminaFill.fillAmount = ratio;
        staminaFill.color = stats.IsExhausted ? lowColor
                          : ratio <= lowThreshold ? Color.Lerp(lowColor, staminaColor, 0.4f)
                          : staminaColor;
    }

    private Bar CreateBar(string name, Vector2 pos, Color color)
    {
        RectTransform barRoot = UIKit.CreateRect(name, root);
        UIKit.SetRect(barRoot, new Vector2(0f, 0f), new Vector2(0f, 0f), pos, barSize);

        Image back = UIKit.CreateImage("Back", barRoot, new Color(0f, 0f, 0f, 0.55f));
        UIKit.SetRect(back.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero, barSize);

        Image fill = UIKit.CreateImage("Fill", barRoot, color);
        UIKit.SetRect(fill.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero, barSize);

        Text label = UIKit.CreateText("Label", barRoot, statFontSize, TextAnchor.MiddleLeft);
        UIKit.SetRect(label.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                      new Vector2(6f, 0f), barSize);

        return new Bar { fill = fill, fillRect = fill.rectTransform, label = label, width = barSize.x };
    }

    private Slot CreateSlot(string name, Vector2 pos, Vector2 size, Vector2 anchor)
    {
        Image bg = UIKit.CreateImage(name, root, new Color(0f, 0f, 0f, 0.55f));
        UIKit.SetRect(bg.rectTransform, anchor, new Vector2(0.5f, 0.5f), pos, size);

        Text label = UIKit.CreateText("Label", bg.transform, slotFontSize, TextAnchor.MiddleCenter);
        UIKit.SetRect(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                      Vector2.zero, size);

        return new Slot { background = bg, label = label };
    }

    private void Update()
    {
        if (canvas == null) return;

        UpdateBars();
        UpdateStaminaGauge();
        UpdateHotbar();
        UpdateCounter();
        UpdateTargetLabel();
        UpdatePickupPrompt();
        UpdateHearthGauges();
    }

    // 배경판을 글자 크기에 맞춘다
    private static void FitBackground(Text text, RectTransform back, Vector2 padding)
    {
        if (text == null || back == null) return;

        float w = text.preferredWidth + padding.x;
        float h = Mathf.Max(text.preferredHeight + padding.y, 20f);
        back.sizeDelta = new Vector2(w, h);

        RectTransform tr = text.rectTransform;
        tr.sizeDelta = new Vector2(w, h);
    }

    private void SetBar(Bar bar, string name, float current, float max, Color color)
    {
        if (bar == null) return;

        float ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        bar.fillRect.sizeDelta = new Vector2(bar.width * ratio, barSize.y);
        bar.fill.color = ratio <= lowThreshold ? lowColor : color;
        bar.label.text = $"{name}  {Mathf.CeilToInt(current)}/{Mathf.RoundToInt(max)}";
    }

    private void UpdateBars()
    {
        if (stats == null) return;

        SetBar(healthBar, "생명력", stats.Health, stats.MaxHealth, healthColor);
        SetBar(warmthBar, "온기", stats.Warmth, stats.MaxWarmth, warmthColor);
        SetBar(hungerBar, "허기", stats.Hunger, stats.MaxHunger, hungerColor);
    }

    private void UpdateHotbar()
    {
        if (hotbar != null)
        {
            for (int i = 0; i < hotbarSlots.Count; i++)
            {
                ItemDef def = hotbar.GetAssigned(i);
                Slot slot = hotbarSlots[i];

                if (def == null) slot.label.text = $"{i + 1}\n-";
                else
                {
                    bool have = inventory != null && inventory.Has(def.kind, 1);
                    slot.label.text = $"{i + 1}\n{def.displayName}" + (have ? "" : " (없음)");
                }

                slot.background.color = (i == hotbar.EquippedIndex)
                    ? new Color(0.95f, 0.85f, 0.35f, 0.55f)
                    : new Color(0f, 0f, 0f, 0.55f);
            }
        }

        if (quickFood != null && foodSlot != null)
        {
            ItemDef def = quickFood.Assigned;
            foodSlot.label.text = def == null
                ? "F\n음식 없음"
                : $"F\n{def.displayName} {quickFood.AssignedCount}";
        }
    }

    private void UpdateCounter()
    {
        if (inventory == null || counterText == null) return;

        int used = 0;
        for (int i = 0; i < inventory.SlotCount; i++)
            if (!inventory.Slots[i].IsEmpty) used++;

        counterText.text = $"칸 {used}/{inventory.SlotCount}" +
                           (inventory.IsFull ? "  — 가득 참" : "") +
                           $"\n골드 {inventory.Gold}";
        counterText.color = inventory.IsFull ? new Color(1f, 0.5f, 0.4f) : Color.white;
        FitBackground(counterText, counterBackRect, new Vector2(24f, 12f));
    }

    private void UpdateTargetLabel()
    {
        if (interactor == null || targetLabel == null) return;

        GameObject labelObject = targetLabelRect.gameObject;

        InteractableBase target = interactor.Current;
        if (target == null || worldCamera == null)
        {
            if (labelObject.activeSelf) labelObject.SetActive(false);
            return;
        }

        Vector3 sp = worldCamera.WorldToScreenPoint(target.Position + Vector3.up * 1.5f);
        if (sp.z < 0f)
        {
            if (labelObject.activeSelf) labelObject.SetActive(false);
            return;
        }

        if (!labelObject.activeSelf) labelObject.SetActive(true);
        targetLabel.text = $"E — {target.Prompt}";
        FitBackground(targetLabel, targetLabelRect, new Vector2(20f, 8f));
        targetLabelRect.anchoredPosition = ScreenToCanvas(sp);
    }

    private void UpdatePickupPrompt()
    {
        if (collector == null || pickupPrompt == null) return;

        GameObject promptObject = pickupPrompt.transform.parent.gameObject;

        bool show = collector.NearbyCount > 0 && (interactor == null || interactor.Current == null);
        if (promptObject.activeSelf != show) promptObject.SetActive(show);
        if (show)
        {
            pickupPrompt.text = $"E — 줍기 (근처 {collector.NearbyCount}묶음)";
            FitBackground(pickupPrompt, pickupBackRect, new Vector2(20f, 8f));
        }
    }

    private void UpdateHearthGauges()
    {
        if (worldCamera == null) return;

        var list = Hearth.All;
        for (int i = 0; i < list.Count; i++)
        {
            Hearth h = list[i];
            if (h == null) continue;

            if (!gauges.TryGetValue(h, out Gauge g)) { g = CreateGauge(h.name); gauges[h] = g; }

            Vector3 sp = worldCamera.WorldToScreenPoint(h.transform.position + Vector3.up * gaugeWorldHeight);
            bool visible = sp.z > 0f;

            // 지금 겨냥 중인 화로라면 라벨이 같은 정보를 보여주므로 게이지를 숨긴다
            if (visible && hideGaugeWhenTargeted && interactor != null && interactor.Current != null)
            {
                Hearth targeted = interactor.Current.GetComponent<Hearth>();
                if (targeted == h) visible = false;
            }
            if (g.root.gameObject.activeSelf != visible) g.root.gameObject.SetActive(visible);
            if (!visible) continue;

            g.root.anchoredPosition = ScreenToCanvas(sp);

            float ratio = Mathf.Clamp01(h.FuelRatio);
            g.fillRect.sizeDelta = new Vector2(GaugeWidth * ratio, GaugeHeight);
            g.fill.color = !h.IsLit ? new Color(0.45f, 0.45f, 0.5f)
                         : ratio <= 0.2f ? new Color(1f, 0.35f, 0.25f)
                         : new Color(1f, 0.65f, 0.2f);

            g.label.text = h.IsLit
                ? (h.IsUpgraded ? "강화 " : "") + $"연료 {Mathf.FloorToInt(h.Fuel)}/{Mathf.FloorToInt(h.FuelCapacity)}"
                : $"{h.DisplayName} 꺼짐";
        }

        // 사라진 화로 정리
        if (gauges.Count > list.Count)
        {
            var stale = new List<Hearth>();
            foreach (var kv in gauges)
                if (kv.Key == null || !list.Contains(kv.Key)) stale.Add(kv.Key);

            for (int i = 0; i < stale.Count; i++)
            {
                if (gauges.TryGetValue(stale[i], out Gauge g) && g.root != null) Destroy(g.root.gameObject);
                gauges.Remove(stale[i]);
            }
        }
    }

    private Gauge CreateGauge(string name)
    {
        RectTransform gaugeRoot = UIKit.CreateRect($"Gauge_{name}", gaugeParent);
        UIKit.SetRect(gaugeRoot, new Vector2(0f, 0f), new Vector2(0.5f, 0f), Vector2.zero,
                      new Vector2(GaugeWidth, GaugeHeight));

        Image back = UIKit.CreateImage("Back", gaugeRoot, new Color(0f, 0f, 0f, 0.55f));
        UIKit.SetRect(back.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero,
                      new Vector2(GaugeWidth, GaugeHeight));

        Image fill = UIKit.CreateImage("Fill", gaugeRoot, new Color(1f, 0.65f, 0.2f));
        UIKit.SetRect(fill.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero,
                      new Vector2(GaugeWidth, GaugeHeight));

        Text label = UIKit.CreateText("Label", gaugeRoot, worldLabelFontSize - 2, TextAnchor.UpperCenter);
        UIKit.SetRect(label.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f),
                      new Vector2(0f, -2f), new Vector2(GaugeWidth + 60f, 18f));

        return new Gauge { root = gaugeRoot, back = back, fill = fill, fillRect = fill.rectTransform, label = label };
    }

    // 스크린 좌표 → 캔버스 좌표(스케일러 보정)
    private Vector2 ScreenToCanvas(Vector3 screenPoint)
    {
        float scale = canvas != null ? canvas.scaleFactor : 1f;
        return new Vector2(screenPoint.x / scale, screenPoint.y / scale);
    }
}