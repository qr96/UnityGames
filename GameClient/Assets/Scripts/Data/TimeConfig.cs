using UnityEngine;

// 시간 설정. 하루 길이와 낮밤 비율을 데이터로 둔다(튜닝 대상).
[CreateAssetMenu(fileName = "TimeConfig", menuName = "혹한/Time Config")]
public class TimeConfig : ScriptableObject
{
    [Header("하루")]
    [Tooltip("하루 길이(실시간 분)")]
    public float dayLengthMinutes = 20f;

    [Tooltip("하루 중 낮이 차지하는 비율 (0.65 = 낮 65% / 밤 35%)")]
    [Range(0.1f, 0.9f)]
    public float dayFraction = 0.65f;

    [Header("시작")]
    [Tooltip("게임 시작 시각 (0 = 자정, 0.25 = 아침 6시에 해당하는 위치)")]
    [Range(0f, 1f)]
    public float startTimeOfDay01 = 0.25f;

    [Header("진행 속도")]
    [Tooltip("게임 시간 흐름 배수. 1이 기본, 디버그용으로 올릴 수 있다")]
    public float timeScale = 1f;

    public float DayLengthSeconds => Mathf.Max(1f, dayLengthMinutes * 60f);
}
