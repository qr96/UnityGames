using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 검기. 직진 투사체를 발사. directionCount로 방사형 발사 가능.
/// - directionCount=1: 정면 1방향 (기본 검기)
/// - directionCount=2+: 방사형 (다방향 검기)
/// 관통은 증가량(누적), 나머지는 절대 배수. 설명은 자동 생성(수동 우선).
/// </summary>
public class SwordWaveActiveSkill : ActiveSkill
{
    [System.Serializable]
    public class LevelStats
    {
        [Tooltip("비워두면 수치에서 자동 생성. 입력하면 우선.")]
        [TextArea(1, 2)]
        public string manualDescription = "";

        [Tooltip("발사 방향 수. 1=정면, 2+=방사형.")]
        public int directionCount = 1;

        [Tooltip("이 레벨에서 추가되는 관통 수 (증가량). 패시브 관통과 합산.")]
        public int pierceBonus = 0;

        [Tooltip("데미지 배수 (절대값)")]
        public float damageMultiplier = 1f;

        [Tooltip("사거리 배수")]
        public float rangeMultiplier = 1f;

        [Tooltip("판정 폭(가로 스케일) 배수")]
        public float widthMultiplier = 1f;

        [Tooltip("투사체 속도 배수")]
        public float speedMultiplier = 1f;
    }

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public float spawnDistance = 1f;

    [Tooltip("기본 관통 수 (Lv1 시작값). 레벨별 pierceBonus가 누적됨.")]
    public int basePierce = 1;

    [Header("Radial Spread")]
    [Tooltip("방사형일 때 방향 사이 각도(도). 360이면 완전 균등 분산, 작으면 부채꼴.")]
    public float spreadArc = 360f;

    [Header("Level Stats (인덱스 0 = Lv1)")]
    public LevelStats[] levelStats = new LevelStats[]
    {
        new LevelStats { directionCount = 1, pierceBonus = 0, damageMultiplier = 1.0f, rangeMultiplier = 1.0f, widthMultiplier = 1.0f, speedMultiplier = 1.0f },
        new LevelStats { directionCount = 1, pierceBonus = 2, damageMultiplier = 1.0f, rangeMultiplier = 1.0f, widthMultiplier = 1.0f, speedMultiplier = 1.0f },
        new LevelStats { directionCount = 1, pierceBonus = 0, damageMultiplier = 1.3f, rangeMultiplier = 1.0f, widthMultiplier = 1.0f, speedMultiplier = 1.0f },
        new LevelStats { directionCount = 1, pierceBonus = 2, damageMultiplier = 1.3f, rangeMultiplier = 1.4f, widthMultiplier = 1.0f, speedMultiplier = 1.0f },
        new LevelStats { directionCount = 1, pierceBonus = 0, damageMultiplier = 1.6f, rangeMultiplier = 1.4f, widthMultiplier = 1.5f, speedMultiplier = 1.0f },
        new LevelStats { directionCount = 1, pierceBonus = 3, damageMultiplier = 1.6f, rangeMultiplier = 1.4f, widthMultiplier = 1.5f, speedMultiplier = 1.4f },
    };

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

    void OnValidate()
    {
        if (levelStats != null && levelStats.Length > 0)
            maxLevel = levelStats.Length;
    }

    LevelStats GetStatsForLevel(int lv)
    {
        if (levelStats == null || levelStats.Length == 0) return new LevelStats();
        int idx = Mathf.Clamp(lv - 1, 0, levelStats.Length - 1);
        return levelStats[idx];
    }

    int GetCumulativePierce(int targetLevel)
    {
        int total = basePierce;
        for (int lv = 1; lv <= targetLevel && lv <= levelStats.Length; lv++)
            total += GetStatsForLevel(lv).pierceBonus;
        return total;
    }

    public override string GetNextLevelDescription()
    {
        int nextLevel = level + 1;
        if (levelStats == null || nextLevel - 1 >= levelStats.Length)
            return description;

        var stats = GetStatsForLevel(nextLevel);
        if (!string.IsNullOrEmpty(stats.manualDescription))
            return stats.manualDescription;

        return BuildAutoDescription(nextLevel);
    }

    string BuildAutoDescription(int targetLevel)
    {
        var cur = GetStatsForLevel(targetLevel);
        var prev = targetLevel > 1 ? GetStatsForLevel(targetLevel - 1) : null;

        var parts = new List<string>();

        // 방향 수 증가
        if (prev != null && cur.directionCount > prev.directionCount)
        {
            int delta = cur.directionCount - prev.directionCount;
            parts.Add($"발사 방향 +{delta} ({cur.directionCount}방향)");
        }

        if (cur.pierceBonus > 0)
        {
            int totalPierce = GetCumulativePierce(targetLevel);
            parts.Add($"관통 +{cur.pierceBonus} (적 {totalPierce}명까지)");
        }

        if (prev == null || cur.damageMultiplier > prev.damageMultiplier + 0.001f)
        {
            float prevMul = prev?.damageMultiplier ?? 1f;
            float delta = (cur.damageMultiplier / prevMul - 1f) * 100f;
            parts.Add($"데미지 +{delta:F0}%");
        }

        if (prev == null || cur.rangeMultiplier > prev.rangeMultiplier + 0.001f)
        {
            float prevMul = prev?.rangeMultiplier ?? 1f;
            float delta = (cur.rangeMultiplier / prevMul - 1f) * 100f;
            parts.Add($"사거리 +{delta:F0}%");
        }

        if (prev == null || cur.widthMultiplier > prev.widthMultiplier + 0.001f)
        {
            float prevMul = prev?.widthMultiplier ?? 1f;
            float delta = (cur.widthMultiplier / prevMul - 1f) * 100f;
            parts.Add($"판정 폭 +{delta:F0}%");
        }

        if (prev == null || cur.speedMultiplier > prev.speedMultiplier + 0.001f)
        {
            float prevMul = prev?.speedMultiplier ?? 1f;
            float delta = (cur.speedMultiplier / prevMul - 1f) * 100f;
            parts.Add($"속도 +{delta:F0}%");
        }

        if (parts.Count == 0) return description;
        return string.Join(", ", parts);
    }

    protected override void Execute(Vector3 playerPosition)
    {
        if (projectilePrefab == null) return;

        LevelStats stats = GetStatsForLevel(level);
        int dirCount = Mathf.Max(1, stats.directionCount);

        if (dirCount == 1)
        {
            // 정면 1방향
            SpawnProjectile(playerPosition, Quaternion.identity, stats);
        }
        else
        {
            // 방사형: spreadArc 범위에 균등 분산
            // 360도면 완전 한 바퀴, 그 외엔 정면 중심 부채꼴
            float totalArc = spreadArc;
            bool fullCircle = Mathf.Approximately(totalArc, 360f);

            // 완전 원이면 마지막이 처음과 겹치지 않게 dirCount로 나눔
            // 부채꼴이면 양 끝 포함해서 (dirCount-1)로 나눔
            float step = fullCircle ? totalArc / dirCount : totalArc / Mathf.Max(1, dirCount - 1);
            float startAngle = fullCircle ? 0f : -totalArc * 0.5f;

            for (int i = 0; i < dirCount; i++)
            {
                float angle = startAngle + step * i;
                Quaternion rot = Quaternion.Euler(0f, angle, 0f);
                SpawnProjectile(playerPosition, rot, stats);
            }
        }
    }

    void SpawnProjectile(Vector3 playerPosition, Quaternion rot, LevelStats stats)
    {
        // 발사 방향 기준으로 spawnDistance만큼 앞에서 생성
        Vector3 forward = rot * Vector3.forward;
        Vector3 spawnPos = playerPosition + forward * spawnDistance;

        GameObject p = Instantiate(projectilePrefab, spawnPos, rot);
        Projectile proj = p.GetComponent<Projectile>();
        if (proj == null) return;

        int finalPierce = GetCumulativePierce(level) + GetPierceBonus();
        int finalDamage = CalculateFinalDamage(stats.damageMultiplier);
        float finalDistance = proj.maxDistance * stats.rangeMultiplier * GetDistanceMultiplier();

        var spawnParams = new ProjectileSpawnParams
        {
            damage = finalDamage,
            pierce = finalPierce,
            maxDistance = finalDistance,
            uniformSize = GetSizeMultiplier(),
            widthScale = stats.widthMultiplier,
            speedScale = stats.speedMultiplier,
        };

        proj.Setup(spawnParams);
    }
}