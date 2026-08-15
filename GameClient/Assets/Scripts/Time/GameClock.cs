using System;
using UnityEngine;

// 게임 시계. 낮/밤·날짜·모든 주기의 단일 기준.
//
// 규칙: 게임플레이 시간(자원 재생, 허기·체온, 연료 소모, 상인 주기)은 이 시계를 쓴다.
//       연출·애니메이션은 실제 시간(Time.deltaTime)을 그대로 쓴다.
//
// 수면으로 시간을 건너뛰면(Skip) 그동안의 소모·재생이 정상 반영된다.
public class GameClock : MonoBehaviour
{
    public static GameClock Instance { get; private set; }

    [SerializeField] private TimeConfig config;

    // 게임 시작부터 흐른 게임 시간(초). 재생·소멸 시각의 기준.
    public double Now { get; private set; }

    // 이번 프레임의 게임 시간 증가분. 스킵 중에는 큰 값이 들어올 수 있다.
    public float DeltaTime { get; private set; }

    public int Day { get; private set; } = 1;

    public event Action<int> OnDayChanged;   // 새 날 시작
    public event Action OnNightStarted;
    public event Action OnDayStarted;

    private float dayStartOffset;
    private bool wasNight;

    // 하루 안에서의 위치 (0~1)
    public float TimeOfDay01
    {
        get
        {
            if (config == null) return 0f;
            float t = (float)(Now / config.DayLengthSeconds) + dayStartOffset;
            return Mathf.Repeat(t, 1f);
        }
    }

    public bool IsNight => config != null && TimeOfDay01 >= config.dayFraction;

    // 표시용 24시간 시각
    public int Hour => Mathf.FloorToInt(TimeOfDay01 * 24f);
    public int Minute => Mathf.FloorToInt(Mathf.Repeat(TimeOfDay01 * 24f, 1f) * 60f);

    private void Awake()
    {
        if (Instance == null) Instance = this;

        if (config == null)
        {
            Debug.LogError("[시계] Time Config가 연결되지 않음 — 시간이 흐르지 않는다");
            enabled = false;
            return;
        }

        dayStartOffset = config.startTimeOfDay01;
        wasNight = IsNight;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (config == null) return;
        Advance(Time.deltaTime * Mathf.Max(0f, config.timeScale));
    }

    // 수면 등으로 시간을 건너뛴다
    public void Skip(float gameSeconds)
    {
        if (gameSeconds <= 0f) return;
        Advance(gameSeconds);
    }

    // 다음 아침까지 건너뛴다 (수면)
    public void SkipToMorning()
    {
        if (config == null) return;

        float target = config.startTimeOfDay01;         // 아침 기준 위치
        float current = TimeOfDay01;
        float remain = target > current ? target - current : 1f - current + target;

        Skip(remain * config.DayLengthSeconds);
    }

    private void Advance(float seconds)
    {
        DeltaTime = seconds;
        if (seconds <= 0f) return;

        int beforeDay = DayIndexOf(Now);
        Now += seconds;
        int afterDay = DayIndexOf(Now);

        if (afterDay != beforeDay)
        {
            Day += afterDay - beforeDay;
            OnDayChanged?.Invoke(Day);
        }

        bool night = IsNight;
        if (night != wasNight)
        {
            wasNight = night;
            if (night) OnNightStarted?.Invoke();
            else OnDayStarted?.Invoke();
        }
    }

    private int DayIndexOf(double now)
        => (int)((now / config.DayLengthSeconds) + dayStartOffset);

    // 시계가 없어도 동작하도록 하는 도우미 (없으면 실제 시간으로 대체)
    public static float Delta => Instance != null ? Instance.DeltaTime : Time.deltaTime;
    public static double Time_ => Instance != null ? Instance.Now : UnityEngine.Time.timeAsDouble;
}
