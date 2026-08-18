using UnityEngine;

/// <summary>
/// 1인칭 조준 중 캐릭터 몸을 숨긴다.
/// 카메라가 머리 위치에 있으므로 몸 렌더러가 화면을 가리기 때문.
/// GameObject를 끄는 게 아니라 렌더러만 꺼서 콜라이더와 로직은 유지한다.
///
/// [세팅] Player 오브젝트에 붙인다. Hide Targets에 캡슐(몸) 렌더러를 꽂는다.
///        무기/Muzzle 비주얼은 넣지 말 것 — 1인칭에서도 보여야 한다.
/// </summary>
public class FirstPersonBodyHider : MonoBehaviour
{
    [Tooltip("조준 중 숨길 렌더러들 (몸통 캡슐 등)")]
    [SerializeField] Renderer[] hideTargets;

    [Tooltip("비워두면 같은 오브젝트에서 자동으로 찾는다")]
    [SerializeField] PlayerController controller;

    bool hidden;

    void Awake()
    {
        if (!controller) controller = GetComponent<PlayerController>();

        if (hideTargets == null || hideTargets.Length == 0)
            Debug.LogWarning($"{name}: Hide Targets가 비어 있다. 숨길 렌더러를 꽂아라.", this);
    }

    void LateUpdate()
    {
        if (!controller) return;

        bool shouldHide = controller.IsAiming;
        if (shouldHide == hidden) return;

        hidden = shouldHide;

        foreach (var r in hideTargets)
            if (r) r.enabled = !hidden;
    }
}
