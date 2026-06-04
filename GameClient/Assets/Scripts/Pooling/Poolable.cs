using UnityEngine;

/// <summary>
/// 풀에서 꺼내진 오브젝트가 자기 자신을 풀로 반납할 수 있게 해주는 컴포넌트.
/// PoolManager가 생성 시 자동으로 부착하므로 프리팹에 미리 달지 않아도 됨.
///
/// 파티클 시스템이 있으면 Stop Action이 Callback으로 자동 설정되어
/// 이펙트가 끝나는 순간 자동 반납됨.
///
/// 이중 반납 방지: 파티클 자동 반납 + 코드 수동 반납이 겹쳐도
/// _released 플래그로 한 번만 반납되도록 보장.
/// </summary>
public class Poolable : MonoBehaviour
{
    /// <summary>이 오브젝트가 속한 풀의 키(원본 프리팹). PoolManager가 설정.</summary>
    [HideInInspector] public GameObject poolKey;

    private bool _released;
    private ParticleSystem[] _particles;   // 재사용 시 잔여 입자 정리용 (자식 포함)

    private void Awake()
    {
        // 루트 파티클이 자연 종료될 때 자동 반납되도록 Stop Action 설정.
        // (자동 반납은 '본체=파티클'인 순수 이펙트용. 자식 트레일에까지 걸면
        //  트레일 종료가 본체를 멋대로 반납시킬 수 있어 루트에만 적용.)
        if (TryGetComponent(out ParticleSystem rootPs))
        {
            var main = rootPs.main;
            main.stopAction = ParticleSystemStopAction.Callback;
        }

        // 클리어 대상은 자식 포함 전체 (트레일/서브이미터 등도 정리).
        _particles = GetComponentsInChildren<ParticleSystem>(true);
    }

    private void OnEnable()
    {
        // 풀에서 꺼낼 때 반납 플래그 리셋 (재사용 대비)
        _released = false;

        // 이전 생애의 잔여 입자 1차 제거.
        // 주의: Prewarm 등으로 '활성화 시점'에 방출되는 입자는 이 시점 이후에 생기므로
        // 여기서 못 지움 → 위치 지정 후 ClearParticles()를 한 번 더 호출해야 함.
        ClearParticles();
    }

    /// <summary>
    /// 자식 포함 모든 파티클의 현재 입자를 즉시 제거.
    /// 위치 지정(Spawn/Setup) '이후'에 호출하면 활성화 순간 옛 위치에서
    /// 방출된 입자(Prewarm 등)까지 확실히 정리된다.
    /// </summary>
    public void ClearParticles()
    {
        if (_particles == null) return;
        for (int i = 0; i < _particles.Length; i++)
        {
            if (_particles[i] != null)
                _particles[i].Clear(true);
        }
    }

    // 파티클 시스템 전용 콜백 (파티클이 없으면 호출되지 않음)
    private void OnParticleSystemStopped()
    {
        ReleaseSelf();
    }

    /// <summary>공용 반납. 몬스터/투사체/코인 등이 직접 호출.</summary>
    public void ReleaseSelf()
    {
        // 이미 반납됐으면 무시 (이중 반납 → collectionCheck 예외 방지)
        if (_released) return;
        _released = true;

        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.Release(poolKey, gameObject);
        }
        else
        {
            // 매니저가 없으면 그냥 파괴 (씬 테스트 등 예외 상황)
            Destroy(gameObject);
        }
    }
}