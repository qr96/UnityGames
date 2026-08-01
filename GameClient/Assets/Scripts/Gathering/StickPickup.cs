using UnityEngine;

// 맨손 나뭇가지 줍기. 1회성 — 주우면 Stick 지급 후 오브젝트 제거.
public class StickPickup : InteractableBase
{
    [SerializeField] private int amount = 1;
    [SerializeField] private Inventory inventory; // 비우면 씬에서 찾음

    private void Start()
    {
        if (inventory == null) inventory = FindObjectOfType<Inventory>();
    }

    public override string Prompt => "줍기";

    public override bool CanInteract(GameObject interactor) => true;

    public override void Interact(GameObject interactor)
    {
        if (inventory == null) inventory = FindObjectOfType<Inventory>();
        if (inventory != null) inventory.Add(ResourceKind.Stick, amount);
        Destroy(gameObject);
    }
}
