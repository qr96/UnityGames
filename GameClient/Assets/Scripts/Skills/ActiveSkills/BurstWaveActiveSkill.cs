using System.Collections;
using UnityEngine;

/// <summary>
/// 연사 검기. projectileCount가 "연사 횟수"로 해석됨.
/// 발동 시 burstInterval 간격으로 1발씩 순차 발사 (종대 연사).
/// 공통 로직은 ProjectileActiveSkill이 담당. 여기선 시간차 발사 패턴만.
/// </summary>
public class BurstWaveActiveSkill : ProjectileActiveSkill
{
    [Header("Burst")]
    [Tooltip("연사 사이 간격(초)")]
    public float burstInterval = 0.12f;

    void Reset()
    {
        tags = new SkillTags
        {
            creation = CreationType.Projectile,
            movement = MovementType.Straight,
            collision = CollisionType.Basic,
            skillClass = SkillClass.Physical,
            element = Element.None,
        };
        maxLevel = 6;
    }

    protected override void Execute(Vector3 playerPosition)
    {
        int shots = GetTotalProjectileCount();  // 레벨 연사 수 + 패시브 보너스
        StartCoroutine(FireBurst(shots));
    }

    IEnumerator FireBurst(int shots)
    {
        for (int i = 0; i < shots; i++)
        {
            // 매 발마다 현재 플레이어 위치 기준 (이동 중에도 따라옴)
            Vector3 origin = PlayerController.PlayerTransform != null
                ? PlayerController.PlayerTransform.position
                : transform.position;

            Vector3 spawnPos = origin + Vector3.forward * spawnDistance;
            SpawnOne(spawnPos, Quaternion.identity);

            if (i < shots - 1)
                yield return new WaitForSeconds(burstInterval);
        }
    }

    // 연사는 발사 수 증가를 "연사"로 표현
    protected override string DescribeProjectileCountIncrease(int delta, int total)
    {
        return $"연사 +{delta} ({total}연사)";
    }
}
