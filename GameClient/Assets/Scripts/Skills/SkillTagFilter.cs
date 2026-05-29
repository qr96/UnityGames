using System;
using UnityEngine;

/// <summary>
/// 태그 필터. 각 필드는 nullable 형태로, 미설정(useXxx=false)이면 와일드카드(전체 매칭).
/// 설정된 필드만 매칭 조건으로 사용.
///
/// 예시:
/// - 모든 액티브에 적용: 아무것도 체크 안 함
/// - 물리 공격에만: useSkillClass=true, skillClass=Physical
/// - 마법 + 화염에만: useSkillClass=true(Magical) + useElement=true(Fire)
/// </summary>
[Serializable]
public class SkillTagFilter
{
    public bool useCreation;
    public CreationType creation;

    public bool useMovement;
    public MovementType movement;

    public bool useCollision;
    public CollisionType collision;

    public bool useSkillClass;
    public SkillClass skillClass;

    public bool useElement;
    public Element element;

    /// <summary>주어진 태그가 이 필터에 매칭되는지.</summary>
    public bool Matches(SkillTags tags)
    {
        if (tags == null) return false;
        if (useCreation && tags.creation != creation) return false;
        if (useMovement && tags.movement != movement) return false;
        if (useCollision && tags.collision != collision) return false;
        if (useSkillClass && tags.skillClass != skillClass) return false;
        if (useElement && tags.element != element) return false;
        return true;
    }
}
