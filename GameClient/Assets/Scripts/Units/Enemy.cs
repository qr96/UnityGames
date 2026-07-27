using UnityEngine;

/// <summary>
/// 적 유닛 (3D). 콜라이더(BoxCollider/CapsuleCollider)는 반드시 "Enemy" 레이어.
/// 3D 모델을 자식으로 붙이고, 콜라이더는 투사체 높이(게임플레이 평면 y)를
/// 반드시 포함하는 크기여야 SphereCast에 걸린다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Enemy : MonoBehaviour
{
    public int maxHp = 30;
    public int CurrentHp { get; private set; }

    [Header("표시 (선택)")]
    [SerializeField] TextMesh hpLabel;        // 프로토타입용. 카메라를 향하도록 회전시켜 배치
    [SerializeField] Renderer body;           // MeshRenderer — 피격 색 변화용

    Color _baseColor;
    public bool IsDead => CurrentHp <= 0;

    public void Init(int hp)
    {
        maxHp = hp;
        CurrentHp = hp;
        RefreshView();
    }

    void Awake()
    {
        if (CurrentHp <= 0) CurrentHp = maxHp;
        if (body != null) _baseColor = body.material.color;
        RefreshView();
    }

    /// <summary>데미지 적용. 이 타격으로 죽었으면 true.</summary>
    public bool TakeDamage(int dmg, bool isCrit)
    {
        if (IsDead) return false;
        CurrentHp -= dmg;
        RefreshView();

        if (CurrentHp <= 0)
        {
            Die();
            return true;
        }

        // 피격 연출 훅 (이후 DOTween/애니메이션으로 교체)
        if (body != null)
            body.material.color = Color.Lerp(Color.red, _baseColor, (float)CurrentHp / maxHp);
        return false;
    }

    void Die()
    {
        // 콜라이더를 즉시 꺼서 같은 프레임의 다른 투사체가 시체에 튕기지 않게 함
        GetComponent<Collider>().enabled = false;
        Destroy(gameObject); // 이후 풀링 + 사망 연출로 교체
    }

    void RefreshView()
    {
        if (hpLabel != null) hpLabel.text = Mathf.Max(0, CurrentHp).ToString();
    }
}
