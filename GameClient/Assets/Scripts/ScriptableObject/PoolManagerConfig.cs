using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 각 오브젝트 풀의 설정을 담는 구조체
/// </summary>
[System.Serializable]
public struct PoolConfig
{
    [Tooltip("Resources 폴더 내의 프리팹 경로 (예: Monster/001)")]
    public string resourcePath;

    [Tooltip("게임 시작 또는 씬 로드시 미리 생성해 둘 초기 개수")]
    public int defaultCapacity;

    [Tooltip("풀이 보관할 수 있는 최대 개수 (넘치면 파괴됨)")]
    public int maxSize;
}

/// <summary>
/// 모든 오브젝트 풀 설정을 담는 ScriptableObject
/// 이 에셋 파일을 생성하여 PoolManager에 연결해야 합니다.
/// </summary>
[CreateAssetMenu(fileName = "PoolConfig", menuName = "ScriptableObjects/Pool Manager Config", order = 1)]
public class PoolManagerConfig : ScriptableObject
{
    public List<PoolConfig> poolSettings = new List<PoolConfig>();
}
