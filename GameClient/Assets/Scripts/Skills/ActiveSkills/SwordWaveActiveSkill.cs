using UnityEngine;

/// <summary>
/// 검기. projectileCount가 발사 "방향 수"로 해석됨.
/// - 1: 정면 1발
/// - 2+: 방사형 (spreadArc 범위에 균등 분산)
/// 공통 로직은 ProjectileActiveSkill이 담당. 여기선 방사 발사 패턴만.
/// </summary>
public class SwordWaveActiveSkill : ProjectileActiveSkill
{
    [Header("Radial Spread")]
    [Tooltip("방사형일 때 분산 각도(도). 360=완전 균등, 작으면 정면 중심 부채꼴.")]
    public float spreadArc = 360f;

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
        int dirCount = GetTotalProjectileCount();  // 레벨 방향 수 + 패시브 보너스

        if (dirCount == 1)
        {
            SpawnInDirection(playerPosition, 0f);
            return;
        }

        bool fullCircle = Mathf.Approximately(spreadArc, 360f);
        float step = fullCircle ? spreadArc / dirCount : spreadArc / Mathf.Max(1, dirCount - 1);
        float startAngle = fullCircle ? 0f : -spreadArc * 0.5f;

        for (int i = 0; i < dirCount; i++)
            SpawnInDirection(playerPosition, startAngle + step * i);
    }

    void SpawnInDirection(Vector3 playerPosition, float angle)
    {
        Quaternion rot = Quaternion.Euler(0f, angle, 0f);
        Vector3 forward = rot * Vector3.forward;
        Vector3 spawnPos = playerPosition + forward * spawnDistance;
        SpawnOne(spawnPos, rot);
    }

    // 검기는 발사 수 증가를 "방향"으로 표현
    protected override string DescribeProjectileCountIncrease(int delta, int total)
    {
        return $"발사 방향 +{delta} ({total}방향)";
    }
}