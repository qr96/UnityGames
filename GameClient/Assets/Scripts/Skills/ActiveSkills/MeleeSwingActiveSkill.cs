using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 정면 부채꼴 근접 공격. 발동 즉시 부채꼴 안의 적에게 데미지.
/// 시각 효과는 별도 프리팹(VisualEffect 부착)으로 잠깐 보였다 사라짐.
/// </summary>
public class MeleeSwingActiveSkill : ActiveSkill
{
    [Header("Range")]
    [Tooltip("검광 닿는 거리(미터)")]
    public float range = 3f;

    [Tooltip("부채꼴 각도(도). 90이면 좌우 45도씩.")]
    public float angle = 90f;

    [Header("Hit Count (레벨 성장)")]
    [Tooltip("기본 타격 가능 적 수")]
    public int baseHitCount = 1;

    [Tooltip("레벨당 추가 타격 적 수")]
    public int hitCountPerLevel = 1;

    [Header("Damage Growth")]
    [Tooltip("레벨당 데미지 증가율 (0.1 = 10%)")]
    public float damagePerLevel = 0.1f;

    [Header("Visual")]
    [Tooltip("검광 이펙트 프리팹 (없으면 시각 효과 없음)")]
    public GameObject visualPrefab;

    [Tooltip("이펙트가 플레이어 정면에서 얼마나 떨어진 곳에 생성될지")]
    public float visualSpawnDistance = 1.5f;

    void Reset()
    {
        tags = new SkillTags
        {
            creation = CreationType.Melee,
            movement = MovementType.Stationary,
            collision = CollisionType.Basic,
            skillClass = SkillClass.Physical,
            element = Element.None,
        };

        // 근접은 기본 공격이라 어떤 클래스든 모션을 가질 가능성 높음
        playerAnimTrigger = "attack";
    }

    protected override void Execute(Vector3 playerPosition)
    {
        // 시각 이펙트
        SpawnVisual(playerPosition);

        // 데미지 처리
        int hitCount = baseHitCount + (level - 1) * hitCountPerLevel;
        float levelDmgBonus = 1f + (level - 1) * damagePerLevel;
        int damage = CalculateFinalDamage(levelDmgBonus);

        // 크기 모디파이어는 range에 적용
        float finalRange = range * GetSizeMultiplier();

        FindAndDamageTargets(playerPosition, finalRange, damage, hitCount);
    }

    void SpawnVisual(Vector3 playerPosition)
    {
        if (visualPrefab == null) return;

        Vector3 spawnPos = playerPosition + Vector3.forward * visualSpawnDistance;
        GameObject vfx = Instantiate(visualPrefab, spawnPos, Quaternion.identity);

        // 크기 모디파이어 시각에도 반영
        float sizeMul = GetSizeMultiplier();
        if (Mathf.Abs(sizeMul - 1f) > 0.001f)
            vfx.transform.localScale *= sizeMul;
    }

    void FindAndDamageTargets(Vector3 playerPos, float effectiveRange, int damage, int maxHits)
    {
        // 거리 내 모든 콜라이더 검색
        Collider[] hits = Physics.OverlapSphere(playerPos, effectiveRange);

        // 부채꼴 안의 적만 필터링 + 거리순 정렬
        var candidates = new List<(IDamageable target, float dist)>();

        foreach (var col in hits)
        {
            if (!col.CompareTag("Enemy")) continue;

            IDamageable target = col.GetComponent<IDamageable>();
            if (target == null || target.IsDead) continue;

            Vector3 toTarget = col.transform.position - playerPos;
            toTarget.y = 0f; // 2D 평면에서 각도 검사

            // 부채꼴 각도 검사 (정면 = +Z 기준)
            float angleToTarget = Vector3.Angle(Vector3.forward, toTarget);
            if (angleToTarget > angle * 0.5f) continue;

            candidates.Add((target, toTarget.sqrMagnitude));
        }

        // 가까운 순으로 정렬
        candidates.Sort((a, b) => a.dist.CompareTo(b.dist));

        // 앞에서부터 maxHits명만 데미지
        int actualHits = Mathf.Min(maxHits, candidates.Count);
        for (int i = 0; i < actualHits; i++)
        {
            candidates[i].target.TakeHit(damage);
        }
    }
}