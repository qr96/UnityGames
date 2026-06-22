using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 한 스테이지의 정의. 웨이브 목록 + 보스 + 배경 설정을 한 덩어리로 묶는다.
/// WaveData(웨이브 1개)를 감싸는 상위 계층이며, WaveData 자체는 그대로 재사용.
///
/// 계층: WaveData(웨이브 1개) → StageData(스테이지 1개) → (나중에) 스테이지 목록.
///
/// 에셋으로 만들어 두고, 인게임은 이 데이터를 받아 실행한다.
/// (로비/맵선택은 나중에 "어떤 StageData를 넘길지"만 고르는 역할로 얹힌다.)
/// </summary>
[CreateAssetMenu(fileName = "Stage", menuName = "Runner/Stage Data", order = 0)]
public class StageData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("맵 선택 UI 등에 표시할 이름")]
    public string stageName = "Stage";

    [Header("Waves")]
    [Tooltip("이 스테이지에서 순서대로 진행할 웨이브들")]
    public List<WaveData> waves = new List<WaveData>();

    [Header("Boss")]
    [Tooltip("이 스테이지의 보스 프리팹 (비우면 보스 없이 종료)")]
    public GameObject bossPrefab;

    [Tooltip("모든 웨이브 클리어 후 보스 등장까지 대기 시간")]
    public float bossSpawnDelay = 2f;

    [Tooltip("보스 스폰 위치 (화면 위쪽). 여기서 등장해 Boss.battleZ로 내려옴.")]
    public Vector3 bossSpawnPosition = new Vector3(0f, 0f, 20f);

    [Header("Background (무한 스크롤)")]
    [Tooltip("반복될 바닥 타일 프리팹 (장식물 포함). 비우면 배경 스크롤 없음.")]
    public GameObject groundTilePrefab;

    [Tooltip("배경 스크롤 속도(-Z). 적 속도(기본 8)보다 약간 느리면 깊이감이 생김.")]
    public float scrollSpeed = 6f;

    [Tooltip("동시에 이어붙여 돌려쓸 타일 개수 (보통 2~3).")]
    [Range(2, 5)] public int tileCount = 3;

    [Tooltip("타일 한 칸의 Z 길이(미터). 타일 프리팹의 실제 길이와 일치해야 이음새가 안 생김.")]
    public float tileLength = 20f;

    [Header("Audio (선택)")]
    [Tooltip("이 스테이지 배경음 (없으면 변경 안 함)")]
    public AudioClip bgm;
}
