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
    private Text targetLabel;
    private RectTransform targetLabelRect;
    private Text pickupPrompt;

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
        counterText = UIKit.CreateText("Counter", root, counterFontSize, TextAnchor.UpperRight);
        UIKit.SetRect(counterText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                      new Vector2(-16f, -16f), new Vector2(320f, 48f));

        // ── 타겟 라벨 (월드 추적) ──
        targetLabel = UIKit.CreateText("TargetLabel", root, worldLabelFontSize, TextAnchor.MiddleCenter);
        targetLabelRect = targetLabel.rectTransform;
        UIKit.SetRect(targetLabelRect, new Vector2(0f, 0f), new Vector2(0.5f, 0f),
                      Vector2.zero, new Vector2(300f, 24f));
        targetLabel.gameObject.SetActive(false);

        // ── 줍기 안내 (하단 중앙 위) ──
        pickupPrompt = UIKit.CreateText("PickupPrompt", root, worldLabelFontSize, TextAnchor.MiddleCenter);
        UIKit.SetRect(pickupPrompt.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                      new Vector2(0f, 96f), new Vector2(300f, 24f));
        pickupPrompt.gameObject.SetActive(false);

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

    private void SetBar(Bar bar, string name, float ratio, Color color)
    {
        if (bar == null) return;

        ratio = Mathf.Clamp01(ratio);
        bar.fillRect.sizeDelta = new Vector2(bar.width * ratio, barSize.y);
        bar.fill.color = ratio <= lowThreshold ? lowColor : color;
        bar.label.text = $"{name}  {Mathf.RoundToInt(ratio * 100f)}%";
    }

    private void UpdateBars()
    {
        if (stats == null) return;

        SetBar(healthBar, "생명력", stats.HealthNormalized, healthColor);
        SetBar(warmthBar, "온기", stats.WarmthNormalized, warmthColor);
        SetBar(hungerBar, "허기", stats.HungerNormalized, hungerColor);
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
    }

    private void UpdateTargetLabel()
    {
        if (interactor == null || targetLabel == null) return;

        InteractableBase target = interactor.Current;
        if (target == null || worldCamera == null)
        {
            if (targetLabel.gameObject.activeSelf) targetLabel.gameObject.SetActive(false);
            return;
        }

        Vector3 sp = worldCamera.WorldToScreenPoint(target.Position + Vector3.up * 1.5f);
        if (sp.z < 0f)
        {
            if (targetLabel.gameObject.activeSelf) targetLabel.gameObject.SetActive(false);
            return;
        }

        if (!targetLabel.gameObject.activeSelf) targetLabel.gameObject.SetActive(true);
        targetLabel.text = $"E — {target.Prompt}";
        targetLabelRect.anchoredPosition = ScreenToCanvas(sp);
    }

    private void UpdatePickupPrompt()
    {
        if (collector == null || pickupPrompt == null) return;

        bool show = collector.NearbyCount > 0 && (interactor == null || interactor.Current == null);
        if (pickupPrompt.gameObject.activeSelf != show) pickupPrompt.gameObject.SetActive(show);
        if (show) pickupPrompt.text = $"E — 줍기 (근처 {collector.NearbyCount}묶음)";
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

            Vector3 sp = worldCamera.WorldToScreenPoint(h.transform.position + Vector3.up * 2.2f);
            bool visible = sp.z > 0f;
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