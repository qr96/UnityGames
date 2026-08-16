using UnityEngine;

// 건축 부품 정의. 바닥·벽·문·지붕을 종류로 구분한다.
// 이번 단계에서는 자원 비용이 없다(팔레트에서 골라 바로 설치).
[CreateAssetMenu(fileName = "BuildPartDef", menuName = "혹한/Build Part Def")]
public class BuildPartDef : ScriptableObject
{
    public enum PartKind
    {
        Floor = 0,   // 칸 바닥
        Wall  = 1,   // 칸의 변
        Door  = 2,   // 칸의 변 (별도 타입 — 이후 open/closed 지원)
        Roof  = 3,   // 칸 위 지붕
    }

    [Header("식별자")]
    public string id;                 // "build/wall_wood"
    public string displayName = "부품";

    [Header("종류")]
    public PartKind kind = PartKind.Floor;
    public GameObject prefab;

    [Header("배치 높이")]
    [Tooltip("이 부품이 놓이는 높이(층 바닥 기준, 미터). 지붕은 벽 높이만큼 올린다")]
    public float placementHeight = 0f;

    [Header("문 (kind = Door)")]
    [Tooltip("열린 상태로 시작할지. 프로토타입에서는 통과 가능이면 충분")]
    public bool startsOpen = true;
    [Tooltip("닫혀 있을 때 통행을 막을지 — 이후 여닫기 구현 시 사용")]
    public bool blocksWhenClosed = true;

    public bool IsEdgePart => kind == PartKind.Wall || kind == PartKind.Door;

    // 설치 직후의 통행 차단 여부. 문은 상태에 따라 달라진다.
    public bool BlocksMovementInitially
        => kind == PartKind.Wall || (kind == PartKind.Door && blocksWhenClosed && !startsOpen);
}
