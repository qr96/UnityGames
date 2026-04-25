using UnityEngine;

/// <summary>
/// 투사체 매커니즘 공통 핸들러.
/// SkillData.projectileType에 따라 Linear/Homing/Guaranteed 투사체 초기화.
/// </summary>
public class ProjectileHandler : MonoBehaviour
{
    public void Fire(SkillData skill, Transform origin, Transform target, LayerMask enemyLayer)
    {
        if (target == null) return;

        if (!PoolManager.Instance.TryCreate(skill.effectPrefabPath, out var obj))
        {
            Debug.LogWarning($"[ProjectileHandler] 투사체 프리팹 로드 실패: {skill.effectPrefabPath}");
            return;
        }

        var projectile = obj.GetComponent<SkillProjectile>();
        if (projectile == null)
        {
            Debug.LogError($"[ProjectileHandler] SkillProjectile 컴포넌트 없음: {skill.effectPrefabPath}");
            PoolManager.Instance.Release(skill.effectPrefabPath, obj);
            return;
        }

        var direction = (target.position - origin.position);
        direction.y = 0f;
        direction = direction.normalized;

        int damage = Mathf.RoundToInt(
            (PlayerStats.Instance != null ? PlayerStats.Instance.TotalAttack : 10)
            * skill.GetCurrentDamageMultiplier());

        projectile.Init(
            origin.position,
            direction,
            target,
            skill.projectileType,
            skill.homingRotateSpeed,
            skill.flightDuration,
            damage,
            skill.GetCurrentRange(),
            enemyLayer,
            skill.effectPrefabPath,
            skill.hitEffectPath);
    }
}