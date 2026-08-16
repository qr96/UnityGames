using UnityEngine;
using UnityEditor;

/// <summary>
/// 컨트롤러 검증용 그레이박스 코스를 씬에 생성한다.
/// 메뉴: Tools > Level > Build Test Course
///
/// 반드시 Assets/_Project/Scripts/Editor/ 폴더에 둘 것.
/// (Editor 폴더가 아니면 빌드에 포함되어 컴파일 에러가 난다)
/// </summary>
public static class GreyboxCourseBuilder
{
    const string RootName = "TestCourse";

    [MenuItem("Tools/Level/Build Test Course")]
    public static void Build()
    {
        var old = GameObject.Find(RootName);
        if (old) Object.DestroyImmediate(old);

        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Build Test Course");

        BuildGround(root.transform);
        BuildSlopes(root.transform);
        BuildStairs(root.transform);
        BuildJumpPlatforms(root.transform);
        BuildGaps(root.transform);
        BuildOcclusionArea(root.transform);
        BuildCorridor(root.transform);

        Selection.activeGameObject = root;
        Debug.Log("테스트 코스 생성 완료. 플레이어를 원점 근처에 두고 돌려봐라.");
    }

    // ── 지면 ───────────────────────────────────
    static void BuildGround(Transform parent)
    {
        var g = Group(parent, "Ground");
        Box(g, "Floor", new Vector3(0, -0.5f, 0), new Vector3(80, 1, 80));
    }

    // ── 경사로: Slope Limit 검증 ────────────────
    // 45도가 CharacterController 기본 한계. 그 위아래를 모두 둔다.
    static void BuildSlopes(Transform parent)
    {
        var g = Group(parent, "Slopes");
        float[] angles = { 15f, 25f, 35f, 45f, 55f };
        float z = 12f;

        for (int i = 0; i < angles.Length; i++)
        {
            float a = angles[i];
            float len = 8f;
            var pos = new Vector3(-24f + i * 7f, Mathf.Sin(a * Mathf.Deg2Rad) * len * 0.5f, z);
            var box = Box(g, $"Slope_{a}deg", pos, new Vector3(4f, 0.5f, len));
            box.transform.rotation = Quaternion.Euler(-a, 0f, 0f);
        }
    }

    // ── 계단: Step Offset 검증 ──────────────────
    // 기본 Step Offset은 0.3. 그보다 낮은/높은 계단을 나란히 둔다.
    static void BuildStairs(Transform parent)
    {
        var g = Group(parent, "Stairs");
        float[] heights = { 0.15f, 0.25f, 0.35f, 0.5f };
        float x = -24f;

        foreach (float h in heights)
        {
            var set = Group(g, $"Stairs_{h:0.00}m");
            for (int i = 0; i < 8; i++)
            {
                Box(set, $"Step_{i}",
                    new Vector3(x, h * i + h * 0.5f, -12f - i * 0.8f),
                    new Vector3(4f, h, 0.8f));
            }
            x += 7f;
        }
    }

    // ── 점프 플랫폼: jumpHeight 검증 ────────────
    static void BuildJumpPlatforms(Transform parent)
    {
        var g = Group(parent, "JumpPlatforms");
        float[] heights = { 0.8f, 1.2f, 1.6f, 2.0f };

        for (int i = 0; i < heights.Length; i++)
        {
            float h = heights[i];
            Box(g, $"Platform_{h}m",
                new Vector3(16f + i * 5f, h * 0.5f, 8f),
                new Vector3(3f, h, 3f));
        }
    }

    // ── 간격 점프: 수평 점프 거리 검증 ──────────
    static void BuildGaps(Transform parent)
    {
        var g = Group(parent, "GapJumps");
        float[] gaps = { 2f, 3f, 4f, 5f };
        float z = -26f;
        float x = -12f;

        Box(g, "Launch", new Vector3(x, 1f, z), new Vector3(4f, 2f, 4f));

        foreach (float gap in gaps)
        {
            x += 4f + gap;
            Box(g, $"Landing_gap{gap}m", new Vector3(x, 1f, z), new Vector3(4f, 2f, 4f));
        }
    }

    // ── 카메라 가림 검증: Deoccluder 확인용 ─────
    static void BuildOcclusionArea(Transform parent)
    {
        var g = Group(parent, "Occlusion");

        // 기둥 숲: 캐릭터가 뒤로 돌아가면 카메라가 당겨지는지 본다
        for (int i = 0; i < 5; i++)
            for (int j = 0; j < 3; j++)
                Box(g, $"Pillar_{i}_{j}",
                    new Vector3(-20f + i * 3f, 2.5f, 26f + j * 4f),
                    new Vector3(1f, 5f, 1f));

        // 낮은 벽: 카메라가 넘어가는지 파고드는지
        Box(g, "LowWall", new Vector3(4f, 0.75f, 26f), new Vector3(12f, 1.5f, 0.5f));

        // 높은 벽: 확실한 차단
        Box(g, "HighWall", new Vector3(4f, 3f, 32f), new Vector3(12f, 6f, 0.5f));
    }

    // ── 좁은 통로: 카메라가 가장 잘 깨지는 곳 ───
    static void BuildCorridor(Transform parent)
    {
        var g = Group(parent, "Corridor");
        Box(g, "WallL", new Vector3(22f, 2f, -14f), new Vector3(0.5f, 4f, 16f));
        Box(g, "WallR", new Vector3(25f, 2f, -14f), new Vector3(0.5f, 4f, 16f));
        Box(g, "Ceiling", new Vector3(23.5f, 4f, -14f), new Vector3(3f, 0.5f, 16f));
    }

    // ── 헬퍼 ───────────────────────────────────
    static Transform Group(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = size;
        go.isStatic = true;
        return go;
    }
}
