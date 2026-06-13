using UnityEngine;

/// <summary>
/// 고블린 대장 공격 패턴. Boss와 같은 GameObject에 부착.
/// Boss.OnEnteredBattle()에서 Activate()로 시작되고, 보스 사망 시 자동 정지.
///
/// 패턴 1 — 돌 투척: 현재 플레이어 위치를 조준해 부채꼴로 투사체를 던짐.
///   기존 Projectile 재사용 (프리팹의 targetTag를 "Player"로 설정할 것).
/// 패턴 2 — 부하 소환: 대장이 고블린 졸개를 보스 주변에 불러냄.
///   기존 Enemy/풀 시스템 그대로 재사용.
///
/// 타이머는 Time.deltaTime 누적 방식 → 레벨업 등 timeScale=0 일시정지 중 자동 멈춤.
/// </summary>
[RequireComponent(typeof(Boss))]
public class BossAttack : MonoBehaviour
{
    [Header("Throw Attack (돌 투척)")]
    [Tooltip("던질 투사체 프리팹. Projectile의 targetTag가 'Player'여야 함. PoolManagerConfig 등록 권장.")]
    public GameObject projectilePrefab;

    [Tooltip("투척 주기(초)")]
    public float throwInterval = 2.5f;

    [Tooltip("한 번에 던지는 개수 (부채꼴)")]
    public int projectileCount = 3;

    [Tooltip("부채꼴 전체 각도(도)")]
    public float spreadAngle = 36f;

    [Tooltip("투사체 데미지")]
    public int projectileDamage = 1;

    [Tooltip("투사체 최대 비행 거리")]
    public float projectileMaxDistance = 30f;

    [Tooltip("투척 높이 (보스 발밑 기준)")]
    public float throwHeight = 1.2f;

    [Header("Summon (부하 소환)")]
    [Tooltip("소환할 졸개 프리팹 (Enemy). 비우면 소환 패턴 비활성.")]
    public GameObject minionPrefab;

    [Tooltip("소환 주기(초)")]
    public float summonInterval = 8f;

    [Tooltip("한 번에 소환하는 마리 수")]
    public int summonCount = 2;

    [Tooltip("소환 위치 X 간격")]
    public float summonSpread = 2f;

    [Tooltip("소환 X 범위 (레인 폭에 맞춤)")]
    public float summonMinX = -4f, summonMaxX = 4f;

    private Boss boss;
    private bool active;
    private float throwTimer;
    private float summonTimer;

    void Awake()
    {
        boss = GetComponent<Boss>();
    }

    /// <summary>전투 시작 (Boss.OnEnteredBattle이 호출).</summary>
    public void Activate()
    {
        active = true;
        throwTimer = 0f;
        summonTimer = 0f;
    }

    void Update()
    {
        if (!active) return;
        if (boss == null || boss.IsDead) return;

        float dt = Time.deltaTime;   // timeScale=0이면 0 → 일시정지 중 패턴도 멈춤
        throwTimer += dt;
        summonTimer += dt;

        if (throwTimer >= throwInterval)
        {
            throwTimer = 0f;
            ThrowFan();
        }

        if (minionPrefab != null && summonTimer >= summonInterval)
        {
            summonTimer = 0f;
            SummonMinions();
        }
    }

    // ─── 패턴 1: 돌 투척 (부채꼴) ───

    void ThrowFan()
    {
        if (projectilePrefab == null) return;

        Vector3 origin = transform.position + Vector3.up * throwHeight;

        // 조준: 발사 순간의 플레이어 위치 (없으면 정면 -Z)
        Vector3 aimDir = Vector3.back;
        if (PlayerController.PlayerTransform != null)
        {
            Vector3 to = PlayerController.PlayerTransform.position - origin;
            to.y = 0f;
            if (to.sqrMagnitude > 0.01f) aimDir = to.normalized;
        }

        Quaternion baseRot = Quaternion.LookRotation(aimDir);
        float step = projectileCount > 1 ? spreadAngle / (projectileCount - 1) : 0f;
        float start = projectileCount > 1 ? -spreadAngle * 0.5f : 0f;

        for (int i = 0; i < projectileCount; i++)
        {
            Quaternion rot = baseRot * Quaternion.Euler(0f, start + step * i, 0f);
            SpawnProjectile(origin + rot * Vector3.forward * 1.2f, rot);
        }

        // 투척 애니메이션 트리거 (애니메이터에 추가하면 주석 해제)
        // if (boss.animator != null) boss.animator.SetTrigger("attack");
    }

    void SpawnProjectile(Vector3 pos, Quaternion rot)
    {
        GameObject p = PoolManager.Instance != null
            ? PoolManager.Instance.Get(projectilePrefab)
            : Instantiate(projectilePrefab);
        if (p == null) return;

        p.transform.SetPositionAndRotation(pos, rot);

        // 풀링된 Projectile은 Setup이 필수 (OnSpawn에서 비활성 상태로 시작하므로,
        // Setup이 호출돼야 initialized=true가 되어 움직이기 시작함)
        if (p.TryGetComponent(out Projectile proj))
            proj.Setup(ProjectileSpawnParams.Default(projectileDamage, 1, projectileMaxDistance));
    }

    // ─── 패턴 2: 부하 소환 ───

    void SummonMinions()
    {
        for (int i = 0; i < summonCount; i++)
        {
            float x = transform.position.x + (i - (summonCount - 1) * 0.5f) * summonSpread;
            Vector3 pos = new Vector3(Mathf.Clamp(x, summonMinX, summonMaxX), 0f, transform.position.z);

            GameObject go = PoolManager.Instance != null
                ? PoolManager.Instance.Get(minionPrefab)
                : Instantiate(minionPrefab);
            if (go == null) continue;

            if (go.TryGetComponent(out Enemy enemy))
                enemy.Spawn(pos, Quaternion.identity);   // 상태 리셋 + 위치/물리 동기화
            else
                go.transform.position = pos;
        }

        // 소환 애니메이션/포효 트리거 (있으면 주석 해제)
        // if (boss.animator != null) boss.animator.SetTrigger("summon");
    }
}
