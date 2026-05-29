using UnityEngine;

/// <summary>
/// 시각 효과 프리팹용. 정해진 시간 후 자동 파괴.
/// 검광, 폭발 이펙트, 타격 이펙트 등 잠깐 보였다 사라지는 것들에 사용.
/// </summary>
public class VisualEffect : MonoBehaviour
{
    [Tooltip("이 시간 후 자동 파괴(초)")]
    public float lifeTime = 0.3f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }
}
