using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 근접 공격의 판정만 담당한다.
/// 언제 휘두를지는 PlayerController가, 맞으면 무슨 일이 일어날지는
/// IDamageable 구현체가 결정한다. 이쪽은 "무엇에 맞았는가"만 판단한다.
/// </summary>
public class MeleeWeapon : MonoBehaviour
{
    [Header("Damage")]
    [Tooltip("한 대당 데미지")]
    [SerializeField] float damage = 10f;

    [Tooltip("넉백 세기 (m/s). 0이면 밀리지 않는다")]
    [SerializeField] float knockback = 4f;

    [Header("Hit Detection")]
    [Tooltip("판정 대상 레이어. Damageable만 체크할 것")]
    [SerializeField] LayerMask hitLayers;

    [Tooltip("캐릭터 중심에서 정면으로 떨어진 거리 (m)")]
    [SerializeField] float range = 1.6f;

    [Tooltip("판정 구의 반지름 (m)")]
    [SerializeField] float radius = 0.9f;

    [Tooltip("판정 중심의 높이 (m)")]
    [SerializeField] float heightOffset = 1.0f;

    [Header("Debug")]
    [SerializeField] bool logHits = true;

    /// <summary>실제로 데미지가 들어간 대상만 알린다. 히트스톱, 이펙트, 사운드가 여기 붙는다.</summary>
    public event Action<IDamageable, DamageInfo> OnHit;

    readonly HashSet<IDamageable> hitThisSwing = new HashSet<IDamageable>();
    readonly Collider[] buffer = new Collider[16];

    float flashUntil;

    Vector3 HitCenter =>
        transform.position
        + Vector3.up * heightOffset
        + transform.forward * range;

    void Reset()
    {
        // 컴포넌트를 처음 붙일 때 기본 마스크를 Damageable로 잡아준다
        hitLayers = Layers.DamageableMask;
    }

    /// <summary>한 번의 휘두르기 시작. 중복 타격 기록을 비운다.</summary>
    public void BeginSwing()
    {
        hitThisSwing.Clear();
    }

    /// <summary>판정 구간 동안 매 프레임 호출한다.</summary>
    public void HitCheck()
    {
        Vector3 center = HitCenter;

        int count = Physics.OverlapSphereNonAlloc(
            center, radius, buffer, hitLayers, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            var col = buffer[i];
            if (!col) continue;

            // 자기 자신과 자식은 무시
            if (col.transform.IsChildOf(transform)) continue;

            // 콜라이더가 자식에 붙어 있어도 부모에서 찾는다
            var target = col.GetComponentInParent<IDamageable>();
            if (target == null || !target.IsAlive) continue;

            // 이번 휘두르기에서 이미 맞은 대상은 무시
            if (!hitThisSwing.Add(target)) continue;

            Vector3 point = col.ClosestPoint(center);
            Vector3 dir = transform.forward;

            var info = new DamageInfo(damage, point, dir, knockback, gameObject);
            target.TakeDamage(in info);

            if (logHits)
                Debug.Log($"[Melee] {col.name}에 {damage} 데미지", col);

            flashUntil = Time.time + 0.2f;
            OnHit?.Invoke(target, info);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Time.time < flashUntil
            ? new Color(1f, 0.2f, 0.2f, 0.5f)
            : new Color(1f, 0.8f, 0.2f, 0.25f);
        Gizmos.DrawSphere(HitCenter, radius);

        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(HitCenter, radius);
    }
}