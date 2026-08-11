using UnityEngine;

// 화로 기본값. 강화(1회)는 Hearth 컴포넌트가 직접 갖는다.
[CreateAssetMenu(fileName = "HearthConfig", menuName = "혹한/Hearth Config")]
public class HearthConfig : ScriptableObject
{
    [Header("기본")]
    public float baseWarmthRadius = 5f;
    public float baseFuelCapacity = 100f;  // 장작 용량
    public float fuelBurnPerSec = 1f;       // 켜져 있을 때 소모

}