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

    [Header("생명력")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float health = 100f;
    [Tooltip("체온 0일 때 초당 감소 (100/30초 = 3.33)")]
    [SerializeField] private float healthDrainAtNoWarmth = 3.33f;
    [Tooltip("허기 0일 때 감소 배수 (체온 0의 1/3)")]
    [SerializeField] private float hungerDrainFactor = 0.333f;
    [Tooltip("불 앞 + 허기 충분할 때 초당 회복 (100/60초 = 1.67)")]
    [SerializeField] private float healthRegenPerSec = 1.67f;
    [Tooltip("이 비율 이상일 때만 생명력 회복")]
    [Range(0f, 1f)]
    [SerializeField] private float regenHungerThreshold = 0.5f;

    [Header("기력 (달리기)")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float stamina = 100f;
    [Tooltip("달리는 동안 초당 소모")]
    [SerializeField] private float staminaDrainPerSec = 20f;
    [Tooltip("달리지 않을 때 초당 회복")]
    [SerializeField] private float staminaRegenPerSec = 16.7f;
    [Tooltip("허기가 이 비율 미만이면 최대 기력이 절반으로")]
    [Range(0f, 1f)]
    [SerializeField] private float lowHungerThreshold = 0.3f;
    [Tooltip("허기 부족 시 최대 기력")]
    [SerializeField] private float lowHungerMaxStamina = 50f;
    [Tooltip("탈진 중 이동 속도 배수")]
    [SerializeField] private float exhaustedSpeedMultiplier = 0.8f;

    [Header("쓰러짐/회복")]
    [Tooltip("쓰러질 때 비활성화할 이동 스크립트(보통 PlayerMovement)")]
    [SerializeField] private MonoBehaviour movementToDisable;

    public float Warmth => warmth;
    public float Hunger => hunger;
    public float WarmthNormalized => maxWarmth > 0f ? warmth / maxWarmth : 0f;
    public float Health => health;
    public float HealthNormalized => maxHealth > 0f ? health / maxHealth : 0f;

    public float Stamina => stamina;
    // 허기가 낮으면 최대 기력이 줄어든다
    public float EffectiveMaxStamina
        => HungerNormalized < lowHungerThreshold ? lowHungerMaxStamina : maxStamina;
    public float StaminaNormalized
        => EffectiveMaxStamina > 0f ? stamina / EffectiveMaxStamina : 0f;

    public bool IsSprinting { get; private set; }
    // 완전 고갈되면 가득 찰 때까지 달리기 봉인
    public bool IsExhausted { get; private set; }
    public bool CanSprint => !IsDown && !IsExhausted && stamina > 0f;

    // 이동 속도 배수 (탈진 시 감속)
    public float MoveSpeedMultiplier => IsExhausted ? exhaustedSpeedMultiplier : 1f;
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

    // 이동 쪽에서 매 프레임 알려줌
    public void SetSprinting(bool value)
    {
        IsSprinting = value && CanSprint;
    }

    // 세이브·디버그용
    public void SetHealth(float value) => health = Mathf.Clamp(value, 0f, maxHealth);

    private void Update()
    {
        float dt = Time.deltaTime;

        // ── 기력 ──
        float maxStam = EffectiveMaxStamina;
        if (stamina > maxStam) stamina = maxStam;

        if (IsSprinting && !IsDown)
        {
            stamina = Mathf.Max(0f, stamina - staminaDrainPerSec * dt);
            if (stamina <= 0f) { IsSprinting = false; IsExhausted = true; }
        }
        else
        {
            stamina = Mathf.Min(maxStam, stamina + staminaRegenPerSec * dt);
            if (IsExhausted && stamina >= maxStam - 0.01f) IsExhausted = false;
        }

        // ── 허기 ──
        hunger = Mathf.Max(0f, hunger - hungerDrainPerSec * dt);
        if (hunger <= 0f)
        {
            if (!hungerEmptyFired) { hungerEmptyFired = true; OnHungerEmpty?.Invoke(); }
        }
        else hungerEmptyFired = false;

        bool warm = Hearth.IsPointWarm(transform.position);

        // ── 체온 ──
        if (IsDown || warm)
            warmth = Mathf.Min(maxWarmth, warmth + warmthChargePerSec * dt);
        else
            warmth = Mathf.Max(0f, warmth - warmthDrainPerSec * dt);

        // ── 생명력 ──
        float drain = 0f;
        if (warmth <= 0f) drain += healthDrainAtNoWarmth;
        if (hunger <= 0f) drain += healthDrainAtNoWarmth * hungerDrainFactor;

        if (drain > 0f)
        {
            health = Mathf.Max(0f, health - drain * dt);
        }
        else if (warm && HungerNormalized >= regenHungerThreshold)
        {
            health = Mathf.Min(maxHealth, health + healthRegenPerSec * dt);
        }

        if (IsDown)
        {
            // 화로로 이송된 상태 — 생명력이 조금이라도 차면 복귀
            if (health >= maxHealth * 0.3f) Recover();
            return;
        }

        if (health <= 0f) Down();
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