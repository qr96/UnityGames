using UnityEngine;

/// <summary>
/// 적의 이동 패턴 결정. Enemy와 같은 GameObject에 부착.
/// 자식 클래스(StraightMover, ZigzagMover 등)가 GetVelocity 구현.
///
/// Enemy는 매 프레임 이걸 호출해서 자기 이동 벡터를 결정.
/// </summary>
public abstract class EnemyMover : MonoBehaviour
{
    /// <summary>현재 프레임의 이동 속도 벡터 (방향 + 크기, 초당).</summary>
    public abstract Vector3 GetVelocity();
}
