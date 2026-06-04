/// <summary>
/// 풀링되는 오브젝트의 재사용 시 상태 초기화 훅.
/// 풀에서 꺼낼 때 OnSpawn(), 반납될 때 OnDespawn()이 호출된다.
///
/// Awake/Start는 풀 재사용 시 다시 호출되지 않으므로,
/// "꺼낼 때마다 초기화해야 하는 상태"(HP, 위치, 속도, 플래그 등)는
/// 반드시 OnSpawn()에서 리셋해야 한다.
///
/// 구현은 선택. 풀 매니저가 TryGetComponent로 있을 때만 호출한다.
/// </summary>
public interface IPoolable
{
    /// <summary>풀에서 꺼내져 활성화될 때. 모든 런타임 상태를 초기 상태로 리셋.</summary>
    void OnSpawn();

    /// <summary>풀로 반납되기 직전. 코루틴 정지, 진행 중인 효과 정리 등.</summary>
    void OnDespawn();
}
