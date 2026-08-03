using System;

// 맵 파일 규격(JSON). 외부에서 생성해 이 형식으로만 주면 로더가 씬에 세운다.
//
// 지형 표기는 '행 문자열' 방식이다. rows[0]이 z=0(아래), 문자 하나가 x 한 칸.
//   levels  : 칸의 높이 층. '0'~'9' (없으면 전부 0층)
//   blocked : 통행 불가 칸. '#' = 막힘, 그 외 = 통행 가능 (절벽 몸통·바위벽 등)
//   ramps   : 경사로 칸. '/' = 경사로, 그 외 = 아님 (층이 다른 이웃 칸으로 넘어갈 수 있는 지점)
//
// 통행 규칙: 이웃 칸끼리 층이 같으면 통행 가능. 층이 1 차이면 둘 중 하나가 경사로일 때만 가능.
//            2층 이상 차이는 불가. blocked 칸은 무조건 불가.
//
// 예시:
// {
//   "cellSize": 1.0,
//   "width": 8,
//   "depth": 4,
//   "levels":  ["00000000", "00011111", "00011111", "00011111"],
//   "blocked": ["........", "........", "........", "........"],
//   "ramps":   ["........", ".../....", "........", "........"],
//   "placements": [
//     { "id": "hearth", "x": 2, "z": 1, "rotationY": 0 },
//     { "id": "tree",   "x": 5, "z": 2, "rotationY": 90 }
//   ]
// }
[Serializable]
public class MapData
{
    public float cellSize = 1f;
    public int width = 60;
    public int depth = 60;

    public string[] levels;
    public string[] blocked;
    public string[] ramps;

    public MapPlacement[] placements;
}

[Serializable]
public class MapPlacement
{
    public string id;
    public int x;
    public int z;
    public float rotationY;
}