using System;
using UnityEngine;

// 타겟팅 코어. 사거리 내 '상호작용 가능' 대상 중 최근접 1개를 활성 타겟으로.
// 거리 동률(tieEpsilon 이내)이면 플레이어가 바라보는 방향에 가까운 쪽 선택.
// 활성 타겟에 외곽선(IHighlightable) 토글. E로 확정 실행.
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private float interactRange = 2f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float tieEpsilon = 0.25f; // 거리 동률 판정 여유(월드 단위)

    public InteractableBase Current { get; private set; }
    public event Action<InteractableBase> OnTargetChanged;

    private void Update()
    {
        // 모달 UI가 열려 있으면 타겟을 놓고 상호작용도 받지 않음
        if (UIInputLock.IsBlocked)
        {
            if (Current != null)
            {
                SetHighlight(Current, false);
                Current = null;
                OnTargetChanged?.Invoke(null);
            }
            return;
        }

        UpdateTarget();

        if (Current != null && Input.GetKeyDown(interactKey) && Current.CanInteract(gameObject))
            Current.Interact(gameObject);
    }

    private void UpdateTarget()
    {
        InteractableBase best = null;
        float bestDist = float.MaxValue;
        float bestFacing = -2f;

        Vector3 p = transform.position;
        Vector3 fwd = transform.forward; fwd.y = 0f;
        fwd = fwd.sqrMagnitude > 0.0001f ? fwd.normalized : Vector3.forward;

        var list = InteractableBase.All;
        for (int i = 0; i < list.Count; i++)
        {
            InteractableBase it = list[i];
            if (it == null || !it.CanInteract(gameObject)) continue;

            Vector3 d = it.Position - p; d.y = 0f;
            float dist = d.magnitude;
            if (dist > interactRange) continue;

            float facing = dist > 0.0001f ? Vector3.Dot(fwd, d / dist) : 1f;

            bool better;
            if (best == null) better = true;
            else if (dist < bestDist - tieEpsilon) better = true;              // 확실히 더 가까움
            else if (dist <= bestDist + tieEpsilon) better = facing > bestFacing; // 동률 → 방향으로 결정
            else better = false;

            if (better) { best = it; bestDist = dist; bestFacing = facing; }
        }

        if (best != Current)
        {
            SetHighlight(Current, false);
            Current = best;
            SetHighlight(Current, true);
            OnTargetChanged?.Invoke(Current);
        }
    }

    private static void SetHighlight(InteractableBase it, bool on)
    {
        if (it == null) return;
        IHighlightable h = it.GetComponent<IHighlightable>();
        if (h != null) h.SetHighlighted(on);
    }
}