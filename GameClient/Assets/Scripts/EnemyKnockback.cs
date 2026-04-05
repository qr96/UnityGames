using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EnemyHealth))]
public class EnemyKnockback : MonoBehaviour
{
    [Header("넉백 설정")]
    public float force = 4f;
    public float duration = 0.2f;

    public bool IsKnockedBack { get; private set; }

    NavMeshAgent _agent;
    EnemyHealth _health;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _health = GetComponent<EnemyHealth>();
        _health.OnDamaged += Apply;
    }

    void OnDestroy()
    {
        if (_health != null) _health.OnDamaged -= Apply;
    }

    void Apply(Vector3 hitDir)
    {
        if (IsKnockedBack || hitDir == Vector3.zero) return;
        StartCoroutine(KnockbackCo(hitDir));
    }

    IEnumerator KnockbackCo(Vector3 dir)
    {
        IsKnockedBack = true;
        _agent.isStopped = true;
        _agent.updatePosition = false;

        dir.y = 0f;
        dir.Normalize();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = 1f - (elapsed / duration);
            transform.position += dir * force * t * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }

        _agent.Warp(transform.position);
        _agent.updatePosition = true;
        _agent.isStopped = false;
        IsKnockedBack = false;
    }
}
