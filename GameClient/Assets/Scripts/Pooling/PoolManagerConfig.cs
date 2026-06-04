using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 각 오브젝트 풀의 설정. 프리팹을 직접 연결한다(인스펙터 드래그).
/// 기존 문자열 경로(resourcePath) 방식에서 직접 참조로 전환됨:
/// - 경로 오타로 인한 런타임 null 제거 (드래그라 오타 불가)
/// - Resources 동기 로딩 히치 제거
/// - 참조된 프리팹만 빌드에 포함 (Resources 폴더 통째 포함 X)
/// </summary>
[System.Serializable]
public struct PoolConfig
{
    [Tooltip("풀링할 프리팹. 인스펙터에서 직접 드래그해 연결.")]
    public GameObject prefab;

    [Tooltip("게임 시작 또는 씬 로드시 미리 생성(프리워밍)해 둘 초기 개수")]
    public int prewarmCount;

    [Tooltip("풀이 보관할 수 있는 최대 개수 (넘치면 파괴됨)")]
    public int maxSize;
}

/// <summary>
/// 모든 오브젝트 풀 설정을 담는 ScriptableObject.
/// 이 에셋을 생성해 PoolManager에 연결한다.
/// </summary>
[CreateAssetMenu(fileName = "PoolConfig", menuName = "ScriptableObjects/Pool Manager Config", order = 1)]
public class PoolManagerConfig : ScriptableObject
{
    public List<PoolConfig> poolSettings = new List<PoolConfig>();
}