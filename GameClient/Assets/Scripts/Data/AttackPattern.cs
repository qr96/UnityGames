using UnityEngine;

// 공격 1회의 문맥. 실행기가 채워서 패턴에 넘긴다.
public struct AttackContext
{
    public GameObject attacker;
    public Vector3 origin;   // 판정 시작점(보통 공격자 위치)
    public Vector3 forward;  // 바라보는 방향(XZ 평면)
    public int power;        // 도구 위력 = 타격량
    public float charge01;   // 차지 진행도 0~1 (차지형 무기만 사용)
}

// 공격 판정 방식. 무기(ItemDef)가 이 에셋을 참조한다.
// 반경형·직선형·투사체형 등은 이 클래스를 상속한 별도 에셋으로 추가하며,
// 새 방식이 생겨도 플레이어 쪽 코드는 바뀌지 않는다.
public abstract class AttackPattern : ScriptableObject
{
    [Header("맞힐 수 있는 대상")]
    public HitCategory[] hittableCategories = { HitCategory.Tree };

    public bool CanHit(HitCategory category)
    {
        if (hittableCategories == null) return false;
        for (int i = 0; i < hittableCategories.Length; i++)
            if (hittableCategories[i] == category) return true;
        return false;
    }

    // 차지(홀드 후 발사)형인지. 기본은 아니오 — 누르면 바로 나간다.
    public virtual bool IsCharged => false;
    // 완전 차지까지 걸리는 시간(초)
    public virtual float ChargeSeconds => 0f;

    // 조준 원뿔(방향·각도·사거리). 차지 상태에 따라 각도가 좁아진다.
    public virtual bool TryGetAimCone(in AttackContext ctx, out Vector3 dir,
                                      out float angleDeg, out float range)
    {
        dir = ctx.forward; angleDeg = 0f; range = 0f;
        return false;
    }

    // 지금 조준에 걸리는 대표 대상(없으면 null). UI·장착 표시에 사용.
    public abstract IHittable FindPrimary(in AttackContext ctx);

    // 실제 타격 실행.
    public abstract void Execute(in AttackContext ctx);

    // 궤적 연출용 부채꼴 정보. 반경형만 의미가 있다.
    public virtual bool TryGetArc(out float radius, out float angleDeg)
    {
        radius = 0f; angleDeg = 0f;
        return false;
    }

    // 씬뷰 표시용(선택)
    public virtual void DrawGizmos(Transform origin) { }
}