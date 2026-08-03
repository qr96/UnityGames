using System.Collections.Generic;
using UnityEngine;

// 격자 구간 → 지형 메시. 순수 생성부(씬 오브젝트를 모름).
// 서브메시 0 = 층 상판(경사로는 기울어진 면), 서브메시 1 = 절벽 옆면.
public static class TerrainMeshBuilder
{
    private static readonly List<Vector3> verts = new List<Vector3>();
    private static readonly List<Vector3> normals = new List<Vector3>();
    private static readonly List<Vector2> uvs = new List<Vector2>();
    private static readonly List<int> topTris = new List<int>();
    private static readonly List<int> sideTris = new List<int>();

    // 모서리 순서: 0=SW, 1=NW, 2=NE, 3=SE
    private static readonly float[] corner = new float[4];

    public static void Build(WorldGrid grid, Mesh mesh,
                             int minX, int minZ, int maxX, int maxZ,
                             Vector3 chunkOrigin)
    {
        verts.Clear(); normals.Clear(); uvs.Clear();
        topTris.Clear(); sideTris.Clear();

        float cs = grid.CellSize;

        for (int x = minX; x <= maxX; x++)
            for (int z = minZ; z <= maxZ; z++)
            {
                Vector2Int cell = new Vector2Int(x, z);
                if (!grid.InBounds(cell)) continue;

                // 모든 높이를 청크 원점 기준 로컬 Y로 통일 (옆면 계산과 동일 기준)
                float baseY = grid.HeightAt(cell) - chunkOrigin.y;
                FillCornerHeights(grid, cell, baseY);

                // 칸 좌하단(SW) 기준 로컬 좌표 (y는 각 모서리 높이로 대입)
                Vector3 sw = grid.Origin + new Vector3(x * cs, 0f, z * cs) - chunkOrigin;

                AddTop(sw, cs);
                AddSide(grid, cell, new Vector2Int(1, 0), sw, cs, chunkOrigin.y);
                AddSide(grid, cell, new Vector2Int(-1, 0), sw, cs, chunkOrigin.y);
                AddSide(grid, cell, new Vector2Int(0, 1), sw, cs, chunkOrigin.y);
                AddSide(grid, cell, new Vector2Int(0, -1), sw, cs, chunkOrigin.y);
            }

        mesh.Clear();
        mesh.indexFormat = verts.Count > 65000
            ? UnityEngine.Rendering.IndexFormat.UInt32
            : UnityEngine.Rendering.IndexFormat.UInt16;

        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(topTris, 0);
        mesh.SetTriangles(sideTris, 1);
        mesh.RecalculateBounds();
    }

    // 네 모서리 높이. 경사로면 올라가는 쪽 두 모서리를 한 층 올린다.
    private static void FillCornerHeights(WorldGrid grid, Vector2Int cell, float baseY)
    {
        for (int i = 0; i < 4; i++) corner[i] = baseY;

        if (!grid.TryGetRampInfo(cell, out Vector2Int dir, out int index, out int count)) return;

        // 런 전체가 한 층을 오르므로 이 칸은 그 구간만 담당
        float step = grid.LevelHeight / Mathf.Max(1, count);
        float low = baseY + index * step;
        float high = low + step;

        for (int i = 0; i < 4; i++) corner[i] = low;

        if (dir.x > 0) { corner[3] = high; corner[2] = high; } // SE, NE
        else if (dir.x < 0) { corner[0] = high; corner[1] = high; } // SW, NW
        else if (dir.y > 0) { corner[1] = high; corner[2] = high; } // NW, NE
        else { corner[0] = high; corner[3] = high; } // SW, SE
    }

    private static void AddTop(Vector3 sw, float cs)
    {
        Vector3 p0 = new Vector3(sw.x, corner[0], sw.z);       // SW
        Vector3 p1 = new Vector3(sw.x, corner[1], sw.z + cs);  // NW
        Vector3 p2 = new Vector3(sw.x + cs, corner[2], sw.z + cs);  // NE
        Vector3 p3 = new Vector3(sw.x + cs, corner[3], sw.z);       // SE

        int v0 = verts.Count;
        verts.Add(p0); verts.Add(p1); verts.Add(p2); verts.Add(p3);

        Vector3 n = Vector3.Cross(p1 - p0, p3 - p0).normalized;
        if (n.y < 0f) n = -n;
        for (int i = 0; i < 4; i++) normals.Add(n);

        uvs.Add(new Vector2(0f, 0f));
        uvs.Add(new Vector2(0f, 1f));
        uvs.Add(new Vector2(1f, 1f));
        uvs.Add(new Vector2(1f, 0f));

        topTris.Add(v0); topTris.Add(v0 + 1); topTris.Add(v0 + 2);
        topTris.Add(v0); topTris.Add(v0 + 2); topTris.Add(v0 + 3);
    }

    // 이웃이 더 낮으면 그 변에 벽을 세운다. 모서리 높이가 다르면 기울어진 벽이 된다.
    private static void AddSide(WorldGrid grid, Vector2Int cell, Vector2Int dir,
                                Vector3 sw, float cs, float chunkOriginY)
    {
        Vector2Int n = cell + dir;
        float neighborTop = (grid.InBounds(n) ? grid.HeightAt(n) : grid.Origin.y) - chunkOriginY;

        // 변의 두 끝점(모서리 인덱스와 위치)
        int ia, ib;
        Vector3 a, b;
        if (dir.x > 0) { ia = 3; ib = 2; a = new Vector3(sw.x + cs, 0f, sw.z); b = new Vector3(sw.x + cs, 0f, sw.z + cs); }
        else if (dir.x < 0) { ia = 1; ib = 0; a = new Vector3(sw.x, 0f, sw.z + cs); b = new Vector3(sw.x, 0f, sw.z); }
        else if (dir.y > 0) { ia = 2; ib = 1; a = new Vector3(sw.x + cs, 0f, sw.z + cs); b = new Vector3(sw.x, 0f, sw.z + cs); }
        else { ia = 0; ib = 3; a = new Vector3(sw.x, 0f, sw.z); b = new Vector3(sw.x + cs, 0f, sw.z); }

        float hA = corner[ia];
        float hB = corner[ib];

        // 양쪽 모서리가 모두 이웃 높이 이하면 벽이 필요 없다
        if (hA <= neighborTop + 0.0001f && hB <= neighborTop + 0.0001f) return;

        Vector3 topA = new Vector3(a.x, hA, a.z);
        Vector3 topB = new Vector3(b.x, hB, b.z);
        Vector3 botA = new Vector3(a.x, Mathf.Min(hA, neighborTop), a.z);
        Vector3 botB = new Vector3(b.x, Mathf.Min(hB, neighborTop), b.z);

        Vector3 normal = new Vector3(dir.x, 0f, dir.y);

        int v0 = verts.Count;
        verts.Add(topA); verts.Add(topB); verts.Add(botB); verts.Add(botA);
        for (int i = 0; i < 4; i++) normals.Add(normal);

        uvs.Add(new Vector2(0f, hA - neighborTop));
        uvs.Add(new Vector2(1f, hB - neighborTop));
        uvs.Add(new Vector2(1f, 0f));
        uvs.Add(new Vector2(0f, 0f));

        sideTris.Add(v0); sideTris.Add(v0 + 1); sideTris.Add(v0 + 2);
        sideTris.Add(v0); sideTris.Add(v0 + 2); sideTris.Add(v0 + 3);
    }
}