using UnityEngine;

public class SwordWaveActiveSkill : ActiveSkill
{
    [Header("Projectile")]
    public GameObject projectilePrefab;
    public float spawnDistance = 1f;

    [Header("Spread")]
    public int spreadCount = 1;
    public float spreadAngle = 15f;

    [Header("Front Extra")]
    public int frontExtraCount = 0;

    [Header("Level Growth (검기 고유)")]
    [Tooltip("레벨당 데미지 증가율 (0.1 = 10%)")]
    public float damagePerLevel = 0.1f;

    [Tooltip("레벨당 거리(지속시간) 증가율 (0.1 = 10%)")]
    public float distancePerLevel = 0.1f;

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
    }

    protected override void Execute(Vector3 playerPosition)
    {
        if (projectilePrefab == null) return;

        int spread = Mathf.Max(1, spreadCount);
        float startAngle = -(spread - 1) * 0.5f * spreadAngle;

        for (int i = 0; i < spread; i++)
        {
            float angle = startAngle + i * spreadAngle;
            Quaternion rot = Quaternion.Euler(0f, angle, 0f);
            Vector3 spawnPos = playerPosition + Vector3.forward * spawnDistance;
            SpawnProjectile(spawnPos, rot);
        }

        for (int i = 0; i < frontExtraCount; i++)
        {
            Vector3 spawnPos = playerPosition + Vector3.forward * (spawnDistance + 1f + i * 1f);
            SpawnProjectile(spawnPos, Quaternion.identity);
        }
    }

    void SpawnProjectile(Vector3 pos, Quaternion rot)
    {
        GameObject p = Instantiate(projectilePrefab, pos, rot);
        Projectile proj = p.GetComponent<Projectile>();
        if (proj == null) return;

        // 레벨 보너스 (검기 자체의 성장)
        float levelDmgBonus = 1f + (level - 1) * damagePerLevel;
        float levelDistBonus = 1f + (level - 1) * distancePerLevel;

        // 베이스 클래스 헬퍼 사용 (모디파이어 자동 적용)
        int finalDamage = CalculateFinalDamage(levelDmgBonus);
        int finalPierce = proj.pierceCount + GetPierceBonus();
        float finalLifeTime = proj.lifeTime * levelDistBonus * GetLifeTimeMultiplier();
        float finalSize = GetSizeMultiplier();

        proj.Setup(finalDamage, finalPierce, finalLifeTime, finalSize);
    }
}