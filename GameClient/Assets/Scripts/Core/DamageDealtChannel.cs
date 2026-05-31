using System;
using UnityEngine;

public struct DamageInfo
{
    public Vector3 position;       // 데미지 발생 월드 위치
    public int amount;             // 데미지 양
    public bool isCritical;        // 크리티컬 여부 (확장 대비)

    // 체력바 추적용. 데미지 받은 대상(적). 없으면 null (팝업만 띄움).
    public Enemy source;
    public float hpRatio;          // 대상의 남은 HP 비율 (체력바용)
}

/// <summary>
/// 데미지 발생 전역 채널. 데미지 팝업/체력바가 구독.
/// </summary>
[CreateAssetMenu(menuName = "Runner/Events/Damage Dealt Channel", fileName = "DamageDealtChannel")]
public class DamageDealtChannel : ScriptableObject
{
    public event Action<DamageInfo> OnRaised;

    public void Raise(DamageInfo info) => OnRaised?.Invoke(info);

    void OnDisable() => OnRaised = null;
}