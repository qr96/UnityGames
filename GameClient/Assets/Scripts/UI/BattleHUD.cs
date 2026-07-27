using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 개발용 전투 HUD (절차 생성). 빈 오브젝트에 붙이고 battle/stage만 연결하면
/// Canvas·슬롯·버튼을 런타임에 전부 생성한다 — 프리팹/씬 수작업 없음.
/// 순수 뷰 레이어: BattleManager 상태를 읽고 TrySwitchHero만 호출. 로직 없음.
/// 아트 패스 때 정식 UGUI/UIToolkit 화면으로 교체되는 개발 등급.
///
/// 구성: 하단 파티 슬롯 바(탭=교체) / 좌상단 잔여 적 / 우상단 배속·재시작 / 중앙 결과 패널
/// </summary>
public class BattleHUD : MonoBehaviour
{
    [SerializeField] BattleManager battle;
    [SerializeField] StageManager stage;

    [Header("색상")]
    [SerializeField] Color slotNormal = new Color(0.20f, 0.22f, 0.28f, 0.9f);
    [SerializeField] Color slotSelected = new Color(0.95f, 0.75f, 0.20f, 0.95f);
    [SerializeField] Color slotDepleted = new Color(0.12f, 0.12f, 0.14f, 0.6f);

    Font _font;
    Text _enemyText;
    Text _speedLabel;
    GameObject _resultPanel;
    Text _resultText;

    class Slot { public Button btn; public Image bg; public Text energy; }
    readonly List<Slot> _slots = new List<Slot>();

    void Start() // 이벤트 버스 규칙: 구독은 Start에서
    {
        if (battle == null || stage == null)
        {
            Debug.LogError("[BattleHUD] battle/stage 레퍼런스 미할당", this);
            enabled = false;
            return;
        }

        _font = LoadBuiltinFont();
        EnsureEventSystem();
        BuildUI();

        BattleEvents.TurnStarted += Refresh;
        BattleEvents.TurnEnded += Refresh;
        BattleEvents.ShotFired += _ => Refresh();
        BattleEvents.EnemyKilled += (_, __) => Refresh();
        BattleEvents.StageCleared += () => ShowResult("STAGE CLEAR");
        BattleEvents.StageFailed += () => ShowResult("STAGE FAILED");

        Refresh();
    }

    void OnDestroy()
    {
        // 람다 구독분은 씬 리로드 시 BattleEvents.ClearAll(BattleManager.Awake)로 일괄 정리됨
        BattleEvents.TurnStarted -= Refresh;
        BattleEvents.TurnEnded -= Refresh;
    }

    // ---------------- 상태 갱신 ----------------

    void Refresh()
    {
        var party = battle.Party;
        for (int i = 0; i < _slots.Count && i < party.Count; i++)
        {
            int energy = battle.GetEnergy(party[i].hero);
            _slots[i].energy.text = $"기력 {energy}";
            _slots[i].btn.interactable = energy > 0;
            _slots[i].bg.color = i == battle.CurrentHeroIndex ? slotSelected
                               : energy > 0 ? slotNormal : slotDepleted;
        }
        _enemyText.text = $"적 {stage.RemainingEnemies}";
    }

    void ShowResult(string msg)
    {
        Refresh();
        _resultText.text = msg;
        _resultPanel.SetActive(true);
    }

    void OnSlotTapped(int index)
    {
        battle.TrySwitchHero(index);
        Refresh();
    }

    void OnSpeedToggle()
    {
        Time.timeScale = Mathf.Approximately(Time.timeScale, 1f) ? 2f : 1f;
        _speedLabel.text = $"x{Time.timeScale:0}";
    }

    void OnRestart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ---------------- UI 생성 ----------------

    void BuildUI()
    {
        // Canvas
        var canvasGo = new GameObject("BattleHUD_Canvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        // 하단 파티 슬롯 바
        var bar = MakePanel(canvasGo.transform, "PartyBar",
            anchorMin: new Vector2(0, 0), anchorMax: new Vector2(1, 0),
            offsetMin: new Vector2(20, 20), offsetMax: new Vector2(-20, 240),
            color: new Color(0, 0, 0, 0.25f));
        var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 14;
        layout.padding = new RectOffset(14, 14, 14, 14);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        var party = battle.Party;
        for (int i = 0; i < party.Count; i++)
            _slots.Add(MakeSlot(bar, i, party[i]));

        // 좌상단 잔여 적
        _enemyText = MakeText(canvasGo.transform, "EnemyCount", "적 -", 52, TextAnchor.MiddleLeft);
        SetRect(_enemyText.rectTransform,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -110), new Vector2(430, -30));

        // 우상단 배속 / 재시작
        var speedBtn = MakeButton(canvasGo.transform, "SpeedBtn", "x1", 44, OnSpeedToggle);
        SetRect((RectTransform)speedBtn.transform,
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-330, -120), new Vector2(-190, -30));
        _speedLabel = speedBtn.GetComponentInChildren<Text>();

        var restartBtn = MakeButton(canvasGo.transform, "RestartBtn", "재시작", 40, OnRestart);
        SetRect((RectTransform)restartBtn.transform,
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-170, -120), new Vector2(-30, -30));

        // 중앙 결과 패널 (숨김 시작)
        var panel = MakePanel(canvasGo.transform, "ResultPanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-380, -220), new Vector2(380, 220),
            new Color(0, 0, 0, 0.85f));
        _resultText = MakeText(panel, "ResultText", "", 84, TextAnchor.MiddleCenter);
        SetRect(_resultText.rectTransform, Vector2.zero, Vector2.one, new Vector2(20, 140), new Vector2(-20, -40));
        var panelRestart = MakeButton(panel, "PanelRestart", "재시작", 48, OnRestart);
        SetRect((RectTransform)panelRestart.transform,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-160, 40), new Vector2(160, 140));
        _resultPanel = panel.gameObject;
        _resultPanel.SetActive(false);
    }

    Slot MakeSlot(RectTransform parent, int index, BattleManager.PartyMember m)
    {
        var go = new GameObject($"Slot_{index}", typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var bg = go.GetComponent<Image>();
        bg.color = slotNormal;
        var btn = go.GetComponent<Button>();
        int captured = index;
        btn.onClick.AddListener(() => OnSlotTapped(captured));

        string heroName = m.hero != null
            ? (string.IsNullOrEmpty(m.hero.displayName) ? m.hero.name : m.hero.displayName)
            : "?";
        var nameText = MakeText(go.transform, "Name", $"{heroName} ★{m.star}", 38, TextAnchor.MiddleCenter);
        SetRect(nameText.rectTransform, new Vector2(0, 0.45f), Vector2.one, new Vector2(6, 0), new Vector2(-6, -8));

        var energyText = MakeText(go.transform, "Energy", "기력 -", 34, TextAnchor.MiddleCenter);
        SetRect(energyText.rectTransform, Vector2.zero, new Vector2(1, 0.45f), new Vector2(6, 8), new Vector2(-6, 0));

        return new Slot { btn = btn, bg = bg, energy = energyText };
    }

    // ---------------- 생성 헬퍼 ----------------

    RectTransform MakePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        var go = new GameObject(name, typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        var rt = (RectTransform)go.transform;
        SetRect(rt, anchorMin, anchorMax, offsetMin, offsetMax);
        return rt;
    }

    Text MakeText(Transform parent, string name, string content, int size, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.text = content;
        t.font = _font; // 코드 생성 Text는 폰트 미할당 시 렌더링 안 됨 (TextMesh와 동일한 함정)
        t.fontSize = size;
        t.alignment = anchor;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }

    Button MakeButton(Transform parent, string name, string label, int fontSize, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name, typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.25f, 0.28f, 0.36f, 0.95f);
        var btn = go.GetComponent<Button>();
        btn.onClick.AddListener(onClick);
        var t = MakeText(go.transform, "Label", label, fontSize, TextAnchor.MiddleCenter);
        SetRect(t.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return btn;
    }

    static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    static Font LoadBuiltinFont()
    {
        try { return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
        catch { }
        try { return Resources.GetBuiltinResource<Font>("Arial.ttf"); }
        catch { return null; }
    }

    static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }
}
