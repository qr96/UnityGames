using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투의 턴 상태머신 (3D).
///
///   Aiming ──(발사)──▶ Firing ──(전 투사체 착지)──▶ Resolving ──▶ Aiming
///                                                      │
///                                     (적 전멸 → Cleared / 기력 전부 소진 → Failed)
///
/// 게임플레이 평면: y = planeHeight 인 XZ 평면.
/// 발사 라인: z = launchLineZ. 착지한 x좌표가 다음 발사 x좌표가 된다 (규칙 8).
/// </summary>
public class BattleManager : MonoBehaviour
{
    public enum State { Aiming, Firing, Resolving, Cleared, Failed }

    [Header("참조")]
    [SerializeField] Projectile projectilePrefab;
    [SerializeField] Transform launcherVisual;   // 발사 위치 표시 (영웅 모델 자리)
    [SerializeField] StageManager stage;

    [Header("게임플레이 평면")]
    [Tooltip("투사체가 이동하는 높이. 적 콜라이더가 이 높이를 포함해야 함.")]
    [SerializeField] float planeHeight = 0.5f;
    [SerializeField] float launchLineZ = -6f;    // 발사 라인의 z좌표
    [SerializeField] float launchXMin = -2.6f;   // 필드 밖 방지 클램프
    [SerializeField] float launchXMax = 2.6f;

    [Header("발사 설정")]
    [SerializeField] LayerMask collisionMask;    // Wall | Enemy
    [SerializeField] int randomSeed = 12345;     // 치명타 등 결정적 재현용

    [Header("파티 (프로토타입: 인스펙터에서 직접 지정. 이후 세이브 데이터가 공급)")]
    [SerializeField] List<PartyMember> party = new List<PartyMember>();

    [System.Serializable]
    public class PartyMember
    {
        public HeroData hero;
        [Range(1, 6)] public int star = 1;
    }

    public State Current { get; private set; } = State.Aiming;
    public bool CanAim => Current == State.Aiming;
    public Vector3 LaunchPosition => new Vector3(_launchX, planeHeight, launchLineZ);
    public HeroData CurrentHero => party[_heroIndex].hero;
    public int CurrentStar => party[_heroIndex].star;
    public float PlaneHeight => planeHeight;
    /// <summary>충돌 마스크 단일 소스. AimController/Validator가 이걸 참조 (중복 설정 방지).</summary>
    public LayerMask CollisionMask => collisionMask;
    /// <summary>HUD 등 뷰 레이어용 읽기 전용 파티 접근.</summary>
    public IReadOnlyList<PartyMember> Party => party;
    public int CurrentHeroIndex => _heroIndex;

    float _launchX = 0f;
    int _heroIndex = 0;
    int _aliveProjectiles = 0;
    float _lastLandingX;
    System.Random _rng;

    // 스테이지 내 영웅별 남은 기력 / 누적 소모 (룬 조건용)
    readonly Dictionary<HeroData, int> _energy = new Dictionary<HeroData, int>();
    readonly Dictionary<HeroData, int> _energySpent = new Dictionary<HeroData, int>();

    // 연속 사용 추적 (효과 트리거용)
    HeroData _lastFiredHero;
    int _consecutiveUses;

    void Awake()
    {
        _rng = new System.Random(randomSeed);
        BattleEvents.ClearAll();
        BattleEvents.ProjectileLanded += OnProjectileLanded;
        BattleEvents.EnemyKilled += OnEnemyKilled;

        foreach (var m in party)
        {
            _energy[m.hero] = m.hero.maxEnergy;
            _energySpent[m.hero] = 0;
        }
    }

    void OnDestroy()
    {
        BattleEvents.ProjectileLanded -= OnProjectileLanded;
        BattleEvents.EnemyKilled -= OnEnemyKilled;
    }

    void Start()
    {
        UpdateLauncherVisual();
        BattleEvents.RaiseTurnStarted();
    }

    // ---------------- 발사 ----------------

    /// <summary>AimController가 호출. 현재 영웅으로 1회 발사 (기력 1 소모).</summary>
    public void RequestFire(Vector3 dir)
    {
        if (Current != State.Aiming) return;

        var hero = CurrentHero;
        int energyBefore = _energy[hero];
        if (energyBefore <= 0) return;

        _energy[hero] = energyBefore - 1;
        _energySpent[hero]++;
        _consecutiveUses = (hero == _lastFiredHero) ? _consecutiveUses + 1 : 1;
        _lastFiredHero = hero;

        // 이번 발사의 활성 효과 확정: 성급 필터 통과분.
        // [룬 합류점] 이후 룬 시스템은 장착 룬의 EffectDefinition을 여기서 이 리스트에 Add.
        var activeEffects = new List<EffectDefinition>();
        hero.CollectActiveEffects(CurrentStar, activeEffects);

        // 발사 1회의 공유 컨텍스트. 분열 자식도 이걸 공유 (효과 카운터 통합)
        var shot = new ShotContext
        {
            hero = hero,
            star = CurrentStar,
            energyAtFire = energyBefore,
            energySpentTotal = _energySpent[hero],
            consecutiveUses = _consecutiveUses,
            isLastUse = energyBefore - 1 == 0,
            rng = _rng,
            effects = activeEffects,
        };
        shot.spawnSubProjectile = (pos, d) => SpawnProjectile(pos, d, shot);

        Current = State.Firing;
        _aliveProjectiles = 0;
        SpawnProjectile(LaunchPosition, dir, shot);
    }

    /// <summary>투사체 생성 (본체·분열 자식 공용). 생존 카운트를 여기서 일원 관리.</summary>
    void SpawnProjectile(Vector3 origin, Vector3 dir, ShotContext shot)
    {
        var proj = Instantiate(projectilePrefab); // TODO: 오브젝트 풀로 교체
        proj.collisionMask = collisionMask;
        proj.launchLineZ = launchLineZ;
        _aliveProjectiles++;
        proj.Launch(origin, dir, shot);
    }

    /// <summary>영웅 교체 (기력 남은 영웅만). UI 버튼에서 호출.</summary>
    public bool TrySwitchHero(int index)
    {
        if (Current != State.Aiming) return false;
        if (index < 0 || index >= party.Count) return false;
        if (_energy[party[index].hero] <= 0) return false;
        _heroIndex = index;
        return true;
    }

    public int GetEnergy(HeroData hero) => _energy.TryGetValue(hero, out var e) ? e : 0;

    // ---------------- 이벤트 처리 ----------------

    void OnProjectileLanded(Projectile p, float x)
    {
        _lastLandingX = x;
        _aliveProjectiles--;
        if (_aliveProjectiles <= 0 && Current == State.Firing)
            Resolve();
    }

    void OnEnemyKilled(Projectile p, Enemy e)
    {
        stage.NotifyEnemyKilled(e);
    }

    // ---------------- 턴 정산 ----------------

    void Resolve()
    {
        Current = State.Resolving;

        // 규칙 8: 착지 x좌표 = 다음 발사 x좌표
        _launchX = Mathf.Clamp(_lastLandingX, launchXMin, launchXMax);
        UpdateLauncherVisual();

        BattleEvents.RaiseTurnEnded();

        if (stage.RemainingEnemies <= 0)
        {
            Current = State.Cleared;
            BattleEvents.RaiseStageCleared();
            Debug.Log("STAGE CLEAR!");
            return;
        }

        if (TotalRemainingEnergy() <= 0)
        {
            Current = State.Failed;
            BattleEvents.RaiseStageFailed();
            Debug.Log("STAGE FAILED (기력 소진)");
            return;
        }

        // 현재 영웅 기력이 없으면 기력 남은 영웅으로 자동 전환
        if (_energy[CurrentHero] <= 0)
        {
            for (int i = 0; i < party.Count; i++)
                if (_energy[party[i].hero] > 0) { _heroIndex = i; break; }
        }

        Current = State.Aiming;
        BattleEvents.RaiseTurnStarted();
    }

    int TotalRemainingEnergy()
    {
        int sum = 0;
        foreach (var kv in _energy) sum += kv.Value;
        return sum;
    }

    void UpdateLauncherVisual()
    {
        if (launcherVisual != null)
            launcherVisual.position = LaunchPosition;
    }
}