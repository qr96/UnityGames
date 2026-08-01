using System;
using UnityEngine;

// 온기(추위) + 허기. 온기: 화로 반경 내 균일 충전, 밖은 초당 일정 감소.
// 온기 0 → 쓰러짐 → 최근접 화로 이송 → 온기 회복까지 조작 불능(허기 소모는 지속).
[RequireComponent(typeof(CharacterController))]
public class PlayerStats : MonoBehaviour
{
    [Header("온기 (추위)")]
    [SerializeField] private float maxWarmth = 100f;
    [SerializeField] private float warmth = 100f;
    [SerializeField] private float warmthDrainPerSec = 5f;    // 화로 밖
    [SerializeField] private float warmthChargePerSec = 25f;  // 화로 안(균일)

    [Header("허기")]
    [SerializeField] private float maxHunger = 100f;
    [SerializeField] private float hunger = 100f;
    [SerializeField] private float hungerDrainPerSec = 2f;

    [Header("쓰러짐/회복")]
    [Tooltip("이 온기까지 차면 조작 복귀")]
    [SerializeField] private float recoverWarmthThreshold = 40f;
    [Tooltip("쓰러질 때 비활성화할 이동 스크립트(보통 PlayerMovement)")]
    [SerializeField] private MonoBehaviour movementToDisable;

    public float Warmth => warmth;
    public float Hunger => hunger;
    public float WarmthNormalized => maxWarmth > 0f ? warmth / maxWarmth : 0f;
    public float HungerNormalized => maxHunger > 0f ? hunger / maxHunger : 0f;
    public bool IsDown { get; private set; }

    public event Action OnDown;
    public event Action OnRecover;
    public event Action OnHungerEmpty; // 허기 0 진입 시 1회 (쓰러짐 연결은 식량 시스템 차례에)

    private CharacterController controller;
    private bool hungerEmptyFired;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // 허기: 항상 감소 (쓰러진 동안에도 자원 소모 지속)
        hunger = Mathf.Max(0f, hunger - hungerDrainPerSec * dt);
        if (hunger <= 0f)
        {
            if (!hungerEmptyFired) { hungerEmptyFired = true; OnHungerEmpty?.Invoke(); }
        }
        else hungerEmptyFired = false;

        if (IsDown)
        {
            // 화로로 이송된 상태: 온기 충전으로 회복 대기
            warmth = Mathf.Min(maxWarmth, warmth + warmthChargePerSec * dt);
            if (warmth >= recoverWarmthThreshold) Recover();
            return;
        }

        // 평상시 온기
        if (Hearth.IsPointWarm(transform.position))
            warmth = Mathf.Min(maxWarmth, warmth + warmthChargePerSec * dt);
        else
            warmth = Mathf.Max(0f, warmth - warmthDrainPerSec * dt);

        if (warmth <= 0f) Down();
    }

    // 식량(채집) 시스템에서 호출할 허기 회복 진입점
    public void Eat(float amount)
    {
        hunger = Mathf.Clamp(hunger + amount, 0f, maxHunger);
    }

    private void Down()
    {
        if (IsDown) return;
        IsDown = true;

        if (movementToDisable != null) movementToDisable.enabled = false;

        Hearth h = Hearth.Nearest(transform.position);
        if (h != null) TeleportTo(h.transform.position);

        OnDown?.Invoke();
    }

    private void Recover()
    {
        IsDown = false;
        if (movementToDisable != null) movementToDisable.enabled = true;
        OnRecover?.Invoke();
    }

    // 순간이동: CharacterController는 비활성화 후 위치 변경해야 함
    private void TeleportTo(Vector3 worldPos)
    {
        worldPos.y = transform.position.y; // 평지 가정, 높이 유지
        controller.enabled = false;
        transform.position = worldPos;
        controller.enabled = true;
    }
}
