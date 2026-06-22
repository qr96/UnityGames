using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 무한 스크롤 배경. 타일 N개를 -Z 방향으로 흘리고,
/// 화면 뒤(-Z)로 충분히 빠진 타일을 맨 앞(+Z)으로 다시 보내 재활용한다.
/// (적/코인과 같은 -Z 컨베이어 원리. 타일을 무한 생성하지 않고 몇 개만 돌려씀.)
///
/// 타일 프리팹 안에 장식물(나무/바위 등)을 포함시키면 함께 흐른다.
/// 설정(타일/속도/개수/길이)은 StageData에서 받는다.
/// </summary>
public class ScrollingBackground : MonoBehaviour
{
    [Header("Stage (비우면 WaveSpawner.stage 사용)")]
    public StageData stage;

    [Tooltip("타일이 정렬될 기준 Y/X. 보통 0.")]
    public Vector3 originXY = Vector3.zero;

    [Tooltip("가장 앞 타일의 시작 Z (이 Z에서 -Z로 흘러감)")]
    public float frontZ = 40f;

    private readonly List<Transform> _tiles = new List<Transform>();
    private float _scrollSpeed;
    private float _tileLength;
    private float _recycleZ;   // 이 Z보다 뒤로 가면 앞으로 재배치
    private bool _running;

    void Start()
    {
        if (stage == null && WaveSpawner.Instance != null)
            stage = WaveSpawner.Instance.stage;

        if (stage == null || stage.groundTilePrefab == null)
        {
            Debug.LogWarning("[ScrollingBackground] StageData 또는 타일 프리팹이 없어 배경 스크롤을 건너뜁니다.");
            return;
        }

        _scrollSpeed = stage.scrollSpeed;
        _tileLength = Mathf.Max(0.01f, stage.tileLength);

        // 타일 개수 × 길이만큼 앞에서부터 이어붙임
        int count = Mathf.Max(2, stage.tileCount);
        for (int i = 0; i < count; i++)
        {
            float z = frontZ - i * _tileLength;
            var go = Instantiate(stage.groundTilePrefab, transform);
            go.transform.position = new Vector3(originXY.x, originXY.y, z);
            _tiles.Add(go.transform);
        }

        // 가장 뒤 타일이 완전히 사라지는 지점보다 더 뒤로 가면 재활용
        _recycleZ = frontZ - count * _tileLength;
        _running = true;
    }

    void Update()
    {
        if (!_running) return;

        float delta = _scrollSpeed * Time.deltaTime;   // timeScale=0이면 0 → 일시정지 중 멈춤

        for (int i = 0; i < _tiles.Count; i++)
        {
            Transform t = _tiles[i];
            Vector3 p = t.position;
            p.z -= delta;

            // 뒤로 충분히 빠지면 전체 띠의 맨 앞으로 다시 보냄
            if (p.z <= _recycleZ)
                p.z += _tiles.Count * _tileLength;

            t.position = p;
        }
    }
}
