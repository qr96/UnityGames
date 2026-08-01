using System.Collections.Generic;
using UnityEngine;

// 맞을 수 있는 대상의 분류. 휘두름은 같은 분류끼리만 번진다
// (나무를 패면 옆 나무가 맞고, 열매나 다른 종류는 맞지 않음).
public enum HitCategory
{
    Tree = 0,
    Creature = 1,
    Other = 2,
}

// 타격을 받을 수 있는 대상. 채취 노드·(이후) 전투 대상이 함께 구현.
public interface IHittable
{
    Vector3 HitPosition { get; }
    bool CanBeHit { get; }
    HitCategory Category { get; }

    // count만큼 타격을 받는다. attacker = 타격 주체.
    void ApplyHits(int count, GameObject attacker);
}

// 범위 타격이 대상을 찾기 위한 전역 등록소. 구현체가 켜질 때 등록, 꺼질 때 해제.
public static class HittableRegistry
{
    public static readonly List<IHittable> All = new List<IHittable>();

    public static void Register(IHittable h)
    {
        if (h != null && !All.Contains(h)) All.Add(h);
    }

    public static void Unregister(IHittable h)
    {
        All.Remove(h);
    }
}
