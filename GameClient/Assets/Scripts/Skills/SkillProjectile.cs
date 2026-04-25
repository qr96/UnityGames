using UnityEngine;

/// <summary>
/// 스킬 투사체 런타임 동작.
/// Linear:     발사 시점 방향 고정, 직선 이동. 물리 충돌 기반.
/// Homing:     타겟 추적, 매 프레임 방향 재계산. 물리 충돌 기반.
/// Guaranteed: 물리 미사용. 일정 시간 내 무조건 타겟에 도달, 도달 즉시 데미지.
/// </summary>
[RequireComponent(typeof(Poolable))]
public class SkillProjectile : MonoBehaviour
{
    [Header("투사체 설정")]
    public float speed = 12f;

    // SkillData.hitEffectPath가 비었을 때 사용
    const string DefaultHitEffectPath = "Prefabs/Effects/HCFX_Hit_08";

    ProjectileType _type;
    Vector3 _direction;
    Transform _target;
    float _homingRotateSpeed;
    int _damage;
    float _maxRange;
    float _traveledDistance;
    LayerMask _enemyLayer;
    string _poolKey;
    string _hitEffectPath;
    float _remainingTime;       // Guaranteed 전용
    bool _active;

    public void Init(
        Vector3 startPosition,
        Vector3 direction,
        Transform target,
        ProjectileType type,
        float homingRotateSpeed,
        float flightDuration,
        int damage,
        float maxRange,
        LayerMask enemyLayer,
        string poolKey,
        string hitEffectPath)
    {
        transform.position = startPosition;

        _type = type;
        _direction = direction.normalized;
        _target = target;
        _homingRotateSpeed = homingRotateSpeed;
        _remainingTime = flightDuration;
        _damage = damage;
        _maxRange = maxRange;
        _enemyLayer = enemyLayer;
        _poolKey = poolKey;
        _hitEffectPath = hitEffectPath;
        _traveledDistance = 0f;
        _active = true;

        if (_direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(_direction);

        gameObject.SetActive(true);
    }

    void Update()
    {
        if (!_active) return;

        switch (_type)
        {
            case ProjectileType.Linear: MoveLinear(); break;
            case ProjectileType.Homing: MoveHoming(); break;
            case ProjectileType.Guaranteed: MoveGuaranteed(); return; // 자체 도달/소멸 처리
        }

        _traveledDistance += speed * Time.deltaTime;

        if (_traveledDistance >= _maxRange)
            Despawn();
    }

    // ── 이동 ──────────────────────────────────────────────────────────────

    void MoveLinear()
    {
        transform.position += _direction * speed * Time.deltaTime;
    }

    void MoveHoming()
    {
        if (_target == null || !_target.gameObject.activeInHierarchy)
        {
            MoveLinear();
            return;
        }

        var toTarget = (_target.position - transform.position).normalized;
        toTarget.y = 0f;

        _direction = Vector3.RotateTowards(
            _direction,
            toTarget,
            _homingRotateSpeed * Mathf.Deg2Rad * Time.deltaTime,
            0f).normalized;

        transform.position += _direction * speed * Time.deltaTime;

        if (_direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(_direction);
    }

    void MoveGuaranteed()
    {
        // 타겟 사라지면 페이드아웃 (데미지 없이 소멸)
        if (_target == null || !_target.gameObject.activeInHierarchy)
        {
            Despawn();
            return;
        }

        _remainingTime -= Time.deltaTime;

        // 시간 종료 → 강제 도달 처리
        if (_remainingTime <= 0f)
        {
            ResolveGuaranteedHit();
            return;
        }

        // 매 프레임 "남은 거리 / 남은 시간" 만큼 비례 이동
        // 적이 움직여도 자연스럽게 추적되며, 시간 끝에 정확히 도달
        Vector3 toTarget = _target.position - transform.position;
        float fraction = Time.deltaTime / _remainingTime;
        transform.position += toTarget * fraction;

        if (toTarget.sqrMagnitude > 0.0001f)
        {
            var lookDir = toTarget;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(lookDir.normalized);
        }
    }

    // ── 충돌 (Linear/Homing 전용) ────────────────────────────────────────

    void OnTriggerEnter(Collider other)
    {
        if (!_active) return;
        if (_type == ProjectileType.Guaranteed) return; // 물리 미사용
        if (((_enemyLayer.value >> other.gameObject.layer) & 1) == 0) return;

        var damageable = other.GetComponent<IDamageable>();
        if (damageable == null) return;

        var hitDir = (other.transform.position - transform.position).normalized;
        hitDir.y = 0f;

        damageable.TakeDamage(_damage, hitDir);
        PlayerCombat.RaiseOnHit(damageable, _damage);

        SpawnHitEffect(other.transform.position);

        Despawn();
    }

    // ── Guaranteed 도달 처리 ─────────────────────────────────────────────

    void ResolveGuaranteedHit()
    {
        if (_target == null) { Despawn(); return; }

        var damageable = _target.GetComponent<IDamageable>();
        if (damageable != null)
        {
            var hitDir = (_target.position - transform.position).normalized;
            hitDir.y = 0f;

            damageable.TakeDamage(_damage, hitDir);
            PlayerCombat.RaiseOnHit(damageable, _damage);
            SpawnHitEffect(_target.position);
        }

        Despawn();
    }

    void SpawnHitEffect(Vector3 position)
    {
        string path = !string.IsNullOrEmpty(_hitEffectPath) ? _hitEffectPath : DefaultHitEffectPath;

        if (PoolManager.Instance != null && PoolManager.Instance.TryCreate(path, out var effect))
            effect.transform.position = position;
    }

    // ── 소멸 ──────────────────────────────────────────────────────────────

    void Despawn()
    {
        _active = false;
        GetComponent<Poolable>().ReleaseSelf();
    }

    void OnDisable()
    {
        _active = false;
        _traveledDistance = 0f;
        _target = null;
    }
}