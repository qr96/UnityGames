using UnityEngine;

// 모달 UI(격자 등)가 열려 있는 동안 캐릭터 조작을 잠그는 전역 카운터.
// 여러 UI가 겹쳐도 안전하게 동작. 열 때 Push, 닫을 때 Release.
public static class UIInputLock
{
    private static int count;

    public static bool IsBlocked => count > 0;

    public static void Push() => count++;

    public static void Release()
    {
        count = Mathf.Max(0, count - 1);
    }

    public static void Clear() => count = 0;
}
