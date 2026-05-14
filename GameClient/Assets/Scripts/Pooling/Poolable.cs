using UnityEngine;

/// <summary>
/// 풀에서 꺼내진 오브젝트가 자기 자신을 풀로 반납할 수 있게 해주는 컴포넌트.
/// PoolManager.CreatePool 에서 자동으로 부착되므로 프리팹에 미리 안 달아도 됨.
///
/// 파티클 시스템이 있으면 Stop Action 이 Callback 으로 자동 설정되어
/// 이펙트가 끝나는 순간 자동 반납됨.
/// </summary>
public class Poolable : MonoBehaviour
{
    [HideInInspector]
    public string poolKey;

    private void Awake()
    {
        // 이 오브젝트가 파티클 시스템을 가지고 있는지 확인
        if (TryGetComponent(out ParticleSystem ps))
        {
            // 파티클이 자연 종료될 때 자동 반납되도록
            var main = ps.main;
            main.stopAction = ParticleSystemStopAction.Callback;
        }
    }

    // 파티클 시스템 전용 콜백 (파티클이 없으면 호출되지 않음)
    private void OnParticleSystemStopped()
    {
        ReleaseSelf();
    }

    /// <summary>공용 반납. 몬스터/투사체 등은 직접 호출.</summary>
    public void ReleaseSelf()
    {
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.Release(poolKey, this.gameObject);
        }
        else
        {
            // 매니저가 없으면 그냥 파괴 (씬 테스트 등 예외 상황)
            Destroy(this.gameObject);
        }
    }
}
