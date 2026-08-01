using System.Collections.Generic;
using UnityEngine;

// IInteractable을 구현하는 상호작용 대상 공통 베이스.
// OnEnable/OnDisable로 전역 목록에 자동 등록 → PlayerInteractor가 물리 없이 근처 대상을 찾음.
public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    public static readonly List<InteractableBase> All = new List<InteractableBase>();

    protected virtual void OnEnable()  { if (!All.Contains(this)) All.Add(this); }
    protected virtual void OnDisable() { All.Remove(this); }

    public Vector3 Position => transform.position;

    public abstract string Prompt { get; }
    public abstract bool CanInteract(GameObject interactor);
    public abstract void Interact(GameObject interactor);
}
