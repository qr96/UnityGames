using UnityEngine;

/// <summary>
/// 레이어 이름을 문자열로 흩뿌리지 않기 위한 상수 모음.
/// LayerMask.NameToLayer는 매번 문자열 비교를 하므로 캐싱해두는 편이 낫고,
/// 오타가 나면 컴파일 단계에서 잡힌다.
///
/// Project Settings > Tags and Layers 에서 아래 이름 그대로 레이어를 만들 것.
/// </summary>
public static class Layers
{
    public const string EnvironmentName = "Environment";
    public const string DamageableName  = "Damageable";
    public const string PlayerName      = "Player";

    static int environment = -1;
    static int damageable = -1;
    static int player = -1;

    public static int Environment => Cached(ref environment, EnvironmentName);
    public static int Damageable  => Cached(ref damageable,  DamageableName);
    public static int Player      => Cached(ref player,      PlayerName);

    public static LayerMask EnvironmentMask => 1 << Environment;
    public static LayerMask DamageableMask  => 1 << Damageable;
    public static LayerMask PlayerMask      => 1 << Player;

    static int Cached(ref int slot, string name)
    {
        if (slot < 0)
        {
            slot = LayerMask.NameToLayer(name);
            if (slot < 0)
                Debug.LogError($"레이어 '{name}'가 없다. Project Settings > Tags and Layers에서 추가해라.");
        }
        return slot;
    }
}
