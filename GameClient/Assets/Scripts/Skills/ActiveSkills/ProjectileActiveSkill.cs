using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 투사체를 발사하는 액티브 스킬의 공통 베이스.
/// - 레벨별 스탯(LevelStats) 관리
/// - 카드 설명 자동 생성 (수동 우선)
/// - 투사체 1발 생성(SpawnOne)에 레벨 스탯 + 패시브 모디파이어 적용
///
/// 자식은 Execute()에서 "언제, 어디로, 몇 발" 쏠지만 결정.
/// 발사 수 증가 패시브(ProjectileCountBonus)는 GetExtraProjectileCount()로 조회해
/// 각 자식이 자기 발사 패턴에 합산.
/// </summary>
public abstract class ProjectileActiveSkill : ActiveSkill
{
    [System.Serializable]
    public class LevelStats
    {
        [Tooltip("비우면 수치에서 자동 생성. 입력하면 우선.")]
        [TextArea(1, 2)]
        public string manualDescription = "";

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

        [Tooltip("이 레벨의 발사 수 (방향 수 또는 연사 수 등, 자식이 해석).")]
        public int projectileCount = 1;
    }

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public float spawnDistance = 1f;

    [Tooltip("발사 높이 오프셋. 플레이어 발밑(Y0) 기준으로 이만큼 위에서 발사.")]
    public float spawnHeight = 1f;

    [Tooltip("기본 관통 수 (Lv1 시작값). 레벨별 pierceBonus가 누적됨.")]
    public int basePierce = 1;

    [Header("Level Stats (인덱스 0 = Lv1)")]
    public LevelStats[] levelStats = new LevelStats[]
    {
        new LevelStats { projectileCount = 1, pierceBonus = 0, damageMultiplier = 1f, rangeMultiplier = 1f, widthMultiplier = 1f, speedMultiplier = 1f },
    };

    protected virtual void OnValidate()
    {
        if (levelStats != null && levelStats.Length > 0)
            maxLevel = levelStats.Length;
    }

    protected LevelStats GetStatsForLevel(int lv)
    {
        if (levelStats == null || levelStats.Length == 0) return new LevelStats();
        int idx = Mathf.Clamp(lv - 1, 0, levelStats.Length - 1);
        return levelStats[idx];
    }

    protected LevelStats CurrentStats => GetStatsForLevel(level);

    /// <summary>1레벨부터 targetLevel까지 누적된 관통 수.</summary>
    protected int GetCumulativePierce(int targetLevel)
    {
        int total = basePierce;
        for (int lv = 1; lv <= targetLevel && lv <= levelStats.Length; lv++)
            total += GetStatsForLevel(lv).pierceBonus;
        return total;
    }

    /// <summary>패시브로 추가된 발사 수. 자식이 자기 기본 발사 수에 더해 사용.</summary>
    protected int GetExtraProjectileCount()
    {
        return ModifierRegistry.Instance != null
            ? ModifierRegistry.Instance.GetBonusInt(tags, ModifierType.ProjectileCountBonus)
            : 0;
    }

    /// <summary>현재 레벨 발사 수 + 패시브 보너스.</summary>
    protected int GetTotalProjectileCount()
    {
        return Mathf.Max(1, CurrentStats.projectileCount + GetExtraProjectileCount());
    }

    /// <summary>투사체 1발 생성. 레벨 스탯 + 패시브 모디파이어 모두 적용.</summary>
    protected void SpawnOne(Vector3 spawnPos, Quaternion rotation)
    {
        if (projectilePrefab == null) return;

        // 발사 높이 보정 (플레이어 발밑 기준이라 위로 올림)
        spawnPos.y += spawnHeight;

        // 풀에서 꺼냄 (없으면 fallback으로 직접 생성)
        GameObject p = PoolManager.Instance != null
            ? PoolManager.Instance.Get(projectilePrefab)
            : Instantiate(projectilePrefab);
        if (p == null) return;

        p.transform.SetPositionAndRotation(spawnPos, rotation);

        Projectile proj = p.GetComponent<Projectile>();
        if (proj == null) return;

        LevelStats stats = CurrentStats;

        int finalPierce = GetCumulativePierce(level) + GetPierceBonus();
        int finalDamage = CalculateFinalDamage(stats.damageMultiplier);
        float finalDistance = proj.maxDistance * stats.rangeMultiplier * GetDistanceMultiplier();

        proj.Setup(new ProjectileSpawnParams
        {
            damage = finalDamage,
            pierce = finalPierce,
            maxDistance = finalDistance,
            uniformSize = GetSizeMultiplier(),
            widthScale = stats.widthMultiplier,
            speedScale = stats.speedMultiplier,
        });
    }

    // ─── 카드 설명 자동 생성 ───

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

    /// <summary>이전 레벨 대비 변화량으로 설명 생성. 발사 수 표현은 자식이 override 가능.</summary>
    protected virtual string BuildAutoDescription(int targetLevel)
    {
        var cur = GetStatsForLevel(targetLevel);
        var prev = targetLevel > 1 ? GetStatsForLevel(targetLevel - 1) : null;

        var parts = new List<string>();

        // 발사 수 증가 (자식이 표현 방식 결정)
        if (prev != null && cur.projectileCount > prev.projectileCount)
        {
            int delta = cur.projectileCount - prev.projectileCount;
            parts.Add(DescribeProjectileCountIncrease(delta, cur.projectileCount));
        }

        if (cur.pierceBonus > 0)
        {
            int totalPierce = GetCumulativePierce(targetLevel);
            parts.Add($"관통 +{cur.pierceBonus} (적 {totalPierce}명까지)");
        }

        AddMultiplierPart(parts, "데미지", prev?.damageMultiplier ?? 1f, cur.damageMultiplier, prev == null);
        AddMultiplierPart(parts, "사거리", prev?.rangeMultiplier ?? 1f, cur.rangeMultiplier, prev == null);
        AddMultiplierPart(parts, "판정 폭", prev?.widthMultiplier ?? 1f, cur.widthMultiplier, prev == null);
        AddMultiplierPart(parts, "속도", prev?.speedMultiplier ?? 1f, cur.speedMultiplier, prev == null);

        if (parts.Count == 0) return description;
        return string.Join(", ", parts);
    }

    void AddMultiplierPart(List<string> parts, string label, float prevMul, float curMul, bool isFirst)
    {
        if (!isFirst && curMul <= prevMul + 0.001f) return;
        if (isFirst && Mathf.Approximately(curMul, 1f)) return;
        float delta = (curMul / prevMul - 1f) * 100f;
        parts.Add($"{label} +{delta:F0}%");
    }

    /// <summary>발사 수 증가를 어떻게 설명할지. 자식이 패턴에 맞게 override (방향/연사 등).</summary>
    protected virtual string DescribeProjectileCountIncrease(int delta, int total)
    {
        return $"발사 수 +{delta} ({total}발)";
    }
}