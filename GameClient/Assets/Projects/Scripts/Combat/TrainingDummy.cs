using UnityEngine;

/// <summary>
/// 타격 테스트용 허수아비. IDamageable 구현 예시이기도 하다.
/// 적 AI를 붙일 때도 이 인터페이스만 구현하면 공격 코드는 그대로 동작한다.
///
/// 넉백을 받을지 말지는 맞는 쪽이 결정한다. MeleeWeapon은 세기만 전달할 뿐이다.
///
/// 사용: Capsule에 이 컴포넌트를 붙이고 레이어를 Damageable로 설정.
/// </summary>
public class TrainingDummy : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] float maxHealth = 100f;

    [Header("Knockback")]
    [Tooltip("체크 해제하면 넉백을 무시한다. 고정 허수아비, 바위, 나무 등")]
    [SerializeField] bool receiveKnockback = false;

    [Tooltip("넉백이 감쇠하는 속도. 클수록 빨리 멈춘다")]
    [SerializeField] float knockbackDamping = 8f;

    [Header("Feedback")]
    [Tooltip("맞았을 때 잠깐 바뀔 색")]
    [SerializeField] Color hitColor = new Color(1f, 0.35f, 0.3f);
    [SerializeField] float flashDuration = 0.12f;

    float health;
    Vector3 knockbackVelocity;
    float flashTimer;

    Renderer rend;
    MaterialPropertyBlock mpb;
    Color baseColor;

    public bool IsAlive => health > 0f;
    public float HealthRatio => maxHealth > 0f ? health / maxHealth : 0f;

    void Awake()
    {
        health = maxHealth;

        rend = GetComponentInChildren<Renderer>();
        if (rend)
        {
            mpb = new MaterialPropertyBlock();
            baseColor = rend.sharedMaterial ? rend.sharedMaterial.color : Color.white;
        }
    }

    public void TakeDamage(in DamageInfo info)
    {
        if (!IsAlive) return;

        health -= info.Amount;

        if (receiveKnockback && info.Knockback > 0f)
        {
            Vector3 dir = info.Direction;
            dir.y = 0f;
            knockbackVelocity += dir.normalized * info.Knockback;
        }

        flashTimer = flashDuration;

        if (health <= 0f) Die();
    }

    void Update()
    {
        float dt = Time.deltaTime;

        if (knockbackVelocity.sqrMagnitude > 0.0001f)
        {
            transform.position += knockbackVelocity * dt;
            knockbackVelocity = Vector3.MoveTowards(
                knockbackVelocity, Vector3.zero, knockbackDamping * dt);
        }

        if (flashTimer > 0f)
        {
            flashTimer -= dt;
            SetColor(flashTimer > 0f ? hitColor : baseColor);
        }
    }

    void SetColor(Color c)
    {
        if (!rend) return;
        rend.GetPropertyBlock(mpb);
        mpb.SetColor("_BaseColor", c);   // URP
        mpb.SetColor("_Color", c);       // Built-in
        rend.SetPropertyBlock(mpb);
    }

    void Die()
    {
        health = 0f;
        // 지금은 비활성화만. 나중에 사망 연출로 교체
        gameObject.SetActive(false);
    }
}