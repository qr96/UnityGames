using UnityEngine;

/// <summary>
/// 스킬 정적 데이터. 같은 스킬은 이 에셋 하나를 공유.
/// Assets/Data/Skills/ 폴더에 생성.
/// 스킬별 추가 파라미터는 하위 클래스 또는 별도 ScriptableObject로 확장.
/// </summary>
[CreateAssetMenu(fileName = "SkillData", menuName = "RPG/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("기본 정보")]
    public string skillId;
    public string skillName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("분류")]
    public SkillCategory category;
    public SkillMechanic mechanic;
    public ActivationType activation;

    [Header("성장")]
    [Tooltip("이 스킬의 최대 레벨")]
    public int maxLevel = 5;

    [Header("발동 파라미터 (Lv.1 기준)")]
    [Tooltip("쿨타임형일 때 사용 (초)")]
    public float cooldown;
    [Tooltip("레벨당 쿨타임 감소량 (초). 최소 0.1초로 클램프")]
    public float cooldownReductionPerLevel;

    [Tooltip("확률형일 때 사용 (0~1)")]
    [Range(0f, 1f)] public float probability;
    [Tooltip("레벨당 확률 증가량")]
    public float probabilityPerLevel;

    [Header("전투 파라미터 (Lv.1 기준)")]
    [Tooltip("플레이어 공격력 대비 데미지 배율")]
    public float damageMultiplier;
    [Tooltip("레벨당 데미지 배율 증가량")]
    public float damageMultiplierPerLevel;

    [Tooltip("발동 사거리 (m). 이 범위 내 적 없으면 발동 스킵. 레벨 영향 없음")]
    public float range;

    [Header("투사체 설정 (Projectile 매커니즘일 때)")]
    [Tooltip("Linear: 발사 시점 방향 고정 / Homing: 물리 기반 추적 / Guaranteed: 시간 내 무조건 도달")]
    public ProjectileType projectileType;
    [Tooltip("호밍 타입일 때 회전 속도 (deg/s)")]
    public float homingRotateSpeed = 180f;
    [Tooltip("Guaranteed 타입일 때 발사부터 도달까지 걸리는 시간 (초)")]
    public float flightDuration = 0.5f;

    [Header("리소스")]
    [Tooltip("투사체 또는 이펙트 프리팹. Resources 경로로 PoolManager에서 로드.")]
    public string effectPrefabPath;

    [Tooltip("적 적중 시 이펙트 프리팹 (Resources 경로). 비우면 기본 히트 이펙트 사용")]
    public string hitEffectPath;

    // ── 현재 레벨 기반 수치 ──────────────────────────────────────────────
    // 호출부에서 레벨 신경 안 써도 됨. PlayerSkillManager에서 자동 조회.

    public float GetCurrentCooldown() => GetCooldown(CurrentLevel);
    public float GetCurrentProbability() => GetProbability(CurrentLevel);
    public float GetCurrentDamageMultiplier() => GetDamageMultiplier(CurrentLevel);
    public float GetCurrentRange() => GetRange(CurrentLevel);

    int CurrentLevel
    {
        get
        {
            if (PlayerSkillManager.Instance == null) return 1;
            int lv = PlayerSkillManager.Instance.GetLevel(this);
            return lv > 0 ? lv : 1;
        }
    }

    // ── 레벨 지정 계산 (테스트/툴팁/미리보기용) ─────────────────────────

    public float GetCooldown(int level)
    {
        int steps = Mathf.Clamp(level, 1, maxLevel) - 1;
        return Mathf.Max(0.1f, cooldown - cooldownReductionPerLevel * steps);
    }

    public float GetProbability(int level)
    {
        int steps = Mathf.Clamp(level, 1, maxLevel) - 1;
        return Mathf.Clamp01(probability + probabilityPerLevel * steps);
    }

    public float GetDamageMultiplier(int level)
    {
        int steps = Mathf.Clamp(level, 1, maxLevel) - 1;
        return damageMultiplier + damageMultiplierPerLevel * steps;
    }

    public float GetRange(int level) => range;
}