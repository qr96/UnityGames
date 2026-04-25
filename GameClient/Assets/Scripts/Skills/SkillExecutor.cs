using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보유한 액티브 스킬의 발동 로직 처리.
/// 매 프레임 보유 스킬 순회 → 발동 조건 체크 → 매커니즘 핸들러 호출.
/// 스킬 수치는 SkillData.GetCurrent~() 가 현재 레벨 기반으로 반환.
/// </summary>
public class SkillExecutor : MonoBehaviour
{
    [Header("감지")]
    public LayerMask enemyLayer;

    // 스킬별 쿨타임 타이머 (SkillData → 남은 쿨타임)
    readonly Dictionary<SkillData, float> _cooldownTimers = new();

    // 매커니즘 핸들러
    ProjectileHandler _projectileHandler;

    void Awake()
    {
        _projectileHandler = GetComponent<ProjectileHandler>();

        if (PlayerSkillManager.Instance == null)
            Debug.LogError("[SkillExecutor] PlayerSkillManager.Instance가 없습니다.");
    }

    void Update()
    {
        if (PlayerSkillManager.Instance == null) return;

        foreach (var skill in PlayerSkillManager.Instance.GetActives())
            ProcessSkill(skill);
    }

    // ── 발동 처리 ─────────────────────────────────────────────────────────

    void ProcessSkill(SkillData skill)
    {
        switch (skill.activation)
        {
            case ActivationType.Cooldown: ProcessCooldown(skill); break;
            case ActivationType.Constant: ProcessConstant(skill); break;
                // Probability: PlayerCombat.OnHit 이벤트에서 처리 (추후 구현)
                // Conditional: 조건 체크 로직 추후 구현
        }
    }

    void ProcessCooldown(SkillData skill)
    {
        if (!_cooldownTimers.ContainsKey(skill))
            _cooldownTimers[skill] = 0f;

        _cooldownTimers[skill] -= Time.deltaTime;

        if (_cooldownTimers[skill] > 0f) return;

        // 발동 조건 체크 — 사거리 내 적 존재 여부
        var target = FindClosestEnemy(skill.GetCurrentRange());
        if (target == null) return; // 조건 불충족: 쿨타임 소모 안 함

        Fire(skill, target);
        _cooldownTimers[skill] = skill.GetCurrentCooldown();
    }

    void ProcessConstant(SkillData skill)
    {
        Fire(skill, null);
    }

    // ── 핸들러 디스패치 ───────────────────────────────────────────────────

    void Fire(SkillData skill, Transform target)
    {
        switch (skill.mechanic)
        {
            case SkillMechanic.Projectile:
                _projectileHandler?.Fire(skill, transform, target, enemyLayer);
                break;

                // 추후 매커니즘 핸들러 추가
                // case SkillMechanic.Area: _areaHandler?.Execute(skill, transform); break;
        }
    }

    // ── 유틸 ──────────────────────────────────────────────────────────────

    Transform FindClosestEnemy(float range)
    {
        var hits = Physics.OverlapSphere(transform.position, range, enemyLayer);
        if (hits.Length == 0) return null;

        Transform closest = null;
        float minDistSqr = float.MaxValue;

        foreach (var hit in hits)
        {
            float distSqr = (hit.transform.position - transform.position).sqrMagnitude;
            if (distSqr < minDistSqr)
            {
                minDistSqr = distSqr;
                closest = hit.transform;
            }
        }

        return closest;
    }
}