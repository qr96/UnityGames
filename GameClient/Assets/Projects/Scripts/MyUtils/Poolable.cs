using UnityEditor.EditorTools;
using UnityEngine;

public class Poolable : MonoBehaviour
{
    [HideInInspector]
    public string poolKey;

    private void Awake()
    {
        // 이 오브젝트가 파티클 시스템을 가지고 있는지 확인
        if (TryGetComponent(out ParticleSystem ps))
        {
            // 파티클 시스템이 있다면, Stop Action을 Callback으로 변경
            var main = ps.main;
            main.stopAction = ParticleSystemStopAction.Callback;
        }
    }

    // 파티클 시스템 전용 콜백 (파티클이 없으면 절대 호출되지 않음)
    private void OnParticleSystemStopped()
    {
        ReleaseSelf();
    }

    // 공용 반납 함수 (몬스터, 투사체 등은 이걸 직접 호출)
    public void ReleaseSelf()
    {
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.Release(poolKey, this.gameObject);
        }
        else
        {
            // 혹시 매니저가 없는 상황(씬 테스트 등)이라면 그냥 파괴
            Destroy(this.gameObject);
        }
    }
}
