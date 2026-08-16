using System.Collections.Generic;
using UnityEngine;

// 건축 부품 격자. 기존 WorldGrid 점유(자원·설치물)와 별개 층이다.
//  - 바닥·지붕: 칸 단위 (같은 칸에 둘 다 가능)
//  - 벽·문: 칸의 '변' 단위. 저장은 (칸, 북) / (칸, 동) 둘로만 하고
//           남·서는 이웃 칸의 북·동으로 환산해 중복을 없앤다.
//  - 모든 좌표에 층(level)이 포함된다. 지금은 1층만 쓰지만 데이터는 높이를 구분한다.
public class BuildingGrid : MonoBehaviour
{
    public static BuildingGrid Instance { get; private set; }

    public enum Slot { Floor, Roof, WallNorth, WallEast }

    // 층을 포함한 식별자
    public struct Key
    {
        public Vector2Int cell;
        public int level;
        public Slot slot;

        public Key(Vector2Int cell, int level, Slot slot)
        {
            this.cell = cell; this.level = level; this.slot = slot;
        }
    }

    public class Placed
    {
        public BuildPartDef def;
        public GameObject instance;
        public bool isOpen;          // 문 전용 — 열려 있으면 통행 가능

        public bool BlocksMovement
        {
            get
            {
                if (def == null) return false;
                if (def.kind == BuildPartDef.PartKind.Wall) return true;
                if (def.kind == BuildPartDef.PartKind.Door)
                    return def.blocksWhenClosed && !isOpen;
                return false;
            }
        }
    }

    private readonly Dictionary<Key, Placed> placed = new Dictionary<Key, Placed>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ---- 변 정규화 ----
    // 방향(0=북,1=동,2=남,3=서)을 (칸, 북/동) 형태로 바꾼다.
    public static void NormalizeEdge(Vector2Int cell, int dir, out Vector2Int outCell, out Slot outSlot)
    {
        switch (((dir % 4) + 4) % 4)
        {
            case 0: outCell = cell; outSlot = Slot.WallNorth; break;                        // 북
            case 1: outCell = cell; outSlot = Slot.WallEast; break;                         // 동
            case 2: outCell = cell + new Vector2Int(0, -1); outSlot = Slot.WallNorth; break; // 남 = 아래칸의 북
            default: outCell = cell + new Vector2Int(-1, 0); outSlot = Slot.WallEast; break; // 서 = 왼칸의 동
        }
    }

    // 두 이웃 칸 사이의 변
    public static bool TryGetEdgeBetween(Vector2Int a, Vector2Int b,
                                         out Vector2Int cell, out Slot slot)
    {
        Vector2Int d = b - a;
        cell = a; slot = Slot.WallNorth;

        if (d == new Vector2Int(0, 1))  { NormalizeEdge(a, 0, out cell, out slot); return true; }
        if (d == new Vector2Int(1, 0))  { NormalizeEdge(a, 1, out cell, out slot); return true; }
        if (d == new Vector2Int(0, -1)) { NormalizeEdge(a, 2, out cell, out slot); return true; }
        if (d == new Vector2Int(-1, 0)) { NormalizeEdge(a, 3, out cell, out slot); return true; }
        return false;   // 이웃이 아님
    }

    // ---- 조회 ----
    public bool IsOccupied(Vector2Int cell, int level, Slot slot)
        => placed.ContainsKey(new Key(cell, level, slot));

    public Placed Get(Vector2Int cell, int level, Slot slot)
        => placed.TryGetValue(new Key(cell, level, slot), out Placed p) ? p : null;

    public BuildPartDef GetDef(Vector2Int cell, int level, Slot slot)
        => Get(cell, level, slot)?.def;

    // 두 칸 사이가 막혀 있는지 (이동 판정에서 사용)
    public bool IsEdgeBlocked(Vector2Int from, Vector2Int to, int level)
    {
        if (!TryGetEdgeBetween(from, to, out Vector2Int cell, out Slot slot)) return false;

        Placed p = Get(cell, level, slot);
        return p != null && p.BlocksMovement;
    }

    // ---- 설치·철거 ----
    public bool CanPlace(Vector2Int cell, int level, Slot slot) => !IsOccupied(cell, level, slot);

    public bool Place(Vector2Int cell, int level, Slot slot, BuildPartDef def, GameObject instance)
    {
        if (def == null || instance == null) return false;
        if (!CanPlace(cell, level, slot)) return false;

        placed[new Key(cell, level, slot)] = new Placed
        {
            def = def,
            instance = instance,
            isOpen = def.kind == BuildPartDef.PartKind.Door && def.startsOpen,
        };
        return true;
    }

    public bool Remove(Vector2Int cell, int level, Slot slot)
    {
        Key key = new Key(cell, level, slot);
        if (!placed.TryGetValue(key, out Placed p)) return false;

        if (p.instance != null) Destroy(p.instance);
        placed.Remove(key);
        return true;
    }

    // 문 여닫기 (이후 상호작용에서 호출)
    public bool SetDoorOpen(Vector2Int cell, int level, Slot slot, bool open)
    {
        Placed p = Get(cell, level, slot);
        if (p == null || p.def == null || p.def.kind != BuildPartDef.PartKind.Door) return false;

        p.isOpen = open;
        return true;
    }

    public int Count => placed.Count;
}
