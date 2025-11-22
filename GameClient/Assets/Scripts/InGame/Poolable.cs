using UnityEditor.EditorTools;
using UnityEngine;

/// <summary>
/// 풀링된 오브젝트가 자신의 풀 키를 저장하고 스스로 풀에 반납할 수 있도록 하는 컴포넌트
/// PoolManager에 의해 자동으로 부착되며, 직접 조작할 필요는 없습니다.
/// </summary>
public class Poolable : MonoBehaviour
{
    // 이 오브젝트의 풀 키 (예: "Monsters/Orc")
    [HideInInspector] // 인스펙터에서 보이지 않게 처리
    public string poolKey;

    /// <summary>
    /// 오브젝트를 PoolManager에 반납하는 함수
    /// </summary>
    public void ReleaseSelf()
    {
        // poolKey를 사용하여 PoolManager에게 반납을 요청
        PoolManager.Instance.Release(poolKey, this.gameObject);
    }
}
