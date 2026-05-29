using UnityEngine;

/// <summary>
/// 레벨업 시 카드로 표시할 수 있는 선택지의 공통 인터페이스.
/// 패시브 스킬, 액티브 스킬 레벨업, 신규 액티브 스킬 획득 등 모두 이걸 구현.
/// </summary>
public interface ISelectableChoice
{
    string DisplayName { get; }
    string Description { get; }
    Sprite Icon { get; }

    /// <summary>지금 이 선택지를 추첨 후보에 포함시킬 수 있나?</summary>
    bool CanBeOffered { get; }

    /// <summary>선택됐을 때 실행.</summary>
    void Apply();
}
