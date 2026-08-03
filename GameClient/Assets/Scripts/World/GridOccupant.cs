using UnityEngine;

// 격자를 점유하는 설치물. 시작할 때 자기 위치의 칸에 스냅하고 점유를 등록한다.
// 노드(나무·돌)에도 붙일 수 있고, 붙이지 않으면 격자와 무관하게 존재한다.
public class GridOccupant : MonoBehaviour
{
    [Tooltip("차지하는 칸 수 (가로 X, 세로 Z)")]
    [SerializeField] private Vector2Int footprint = new Vector2Int(1, 1);
    [Tooltip("시작 시 칸 중심으로 위치를 맞춤")]
    [SerializeField] private bool snapOnStart = true;

    public Vector2Int Footprint => footprint;
    public Vector2Int Cell { get; private set; }
    public bool Registered { get; private set; }

    private void Start()
    {
        WorldGrid grid = WorldGrid.Instance;
        if (grid == null)
        {
            Debug.LogWarning($"[격자] {name}: WorldGrid가 씬에 없음 — 점유 등록 생략");
            return;
        }

        Cell = grid.WorldToCell(transform.position);

        if (!grid.Occupy(Cell, footprint, gameObject))
        {
            Debug.LogWarning($"[격자] {name}: {Cell} 점유 실패(겹침 또는 범위 밖)");
            return;
        }

        Registered = true;

        if (snapOnStart)
        {
            Vector3 p = grid.CellToWorldCenter(Cell, footprint);
            transform.position = new Vector3(p.x, transform.position.y, p.z);
        }
    }

    private void OnDestroy()
    {
        if (!Registered) return;
        WorldGrid grid = WorldGrid.Instance;
        if (grid != null) grid.Free(Cell, footprint, gameObject);
        Registered = false;
    }

    // 다른 칸으로 옮길 때
    public bool MoveTo(Vector2Int newCell)
    {
        WorldGrid grid = WorldGrid.Instance;
        if (grid == null) return false;
        if (!grid.IsFree(newCell, footprint, gameObject)) return false;

        if (Registered) grid.Free(Cell, footprint, gameObject);
        Cell = newCell;
        Registered = grid.Occupy(Cell, footprint, gameObject);

        Vector3 p = grid.CellToWorldCenter(Cell, footprint);
        transform.position = new Vector3(p.x, transform.position.y, p.z);
        return Registered;
    }
}
