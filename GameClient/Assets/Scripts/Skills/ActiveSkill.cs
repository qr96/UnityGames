using UnityEngine;

public abstract class ActiveSkill : MonoBehaviour
{
    [Header("Display")]
    public string displayName = "New Active";
    [TextArea(2, 4)]
    public string description = "";
    public Sprite icon;

    [Header("Tags")]
    public SkillTags tags = new SkillTags();

    [Header("Level")]
    public int level = 1;
    public int maxLevel = 5;

    [Header("Timing")]
    public float baseCooldown = 1f;

    [Header("Stats")]
    public int baseDamage = 1;

    [Header("Animation")]
    [Tooltip("발동 시 플레이어 Animator에 보낼 트리거 이름. 비우면 안 보냄.")]
    public string playerAnimTrigger = "";

    private float timer = 0f;

    public bool CanLevelUp => level < maxLevel;

    public void Tick(float deltaTime, Vector3 playerPosition)
    {
        timer += deltaTime;

        float cdMul = ModifierRegistry.Instance != null
            ? ModifierRegistry.Instance.GetMultiplier(tags, ModifierType.CooldownMultiplier)
            : 1f;
        float finalCooldown = Mathf.Max(0.05f, baseCooldown * cdMul);

        if (timer >= finalCooldown)
        {
            timer = 0f;
            Execute(playerPosition);
            TriggerPlayerAnimation();
        }
    }

    void TriggerPlayerAnimation()
    {
        if (string.IsNullOrEmpty(playerAnimTrigger)) return;
        if (PlayerController.Instance == null || PlayerController.Instance.animator == null) return;
        PlayerController.Instance.animator.SetTrigger(playerAnimTrigger);
    }

    public void LevelUp()
    {
        if (!CanLevelUp) return;
        level++;
        OnLevelUp();
    }

    protected virtual void OnLevelUp() { }

    protected abstract void Execute(Vector3 playerPosition);

    public ActiveSkillLevelUpChoice AsChoice()
    {
        return new ActiveSkillLevelUpChoice(this, displayName, description, icon);
    }

    // ─── 헬퍼 ───

    protected int CalculateFinalDamage(float levelBonus)
    {
        float dmgMul = ModifierRegistry.Instance != null
            ? ModifierRegistry.Instance.GetMultiplier(tags, ModifierType.DamageMultiplier)
            : 1f;
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * levelBonus * dmgMul));
    }

    protected float GetSizeMultiplier()
    {
        return ModifierRegistry.Instance != null
            ? ModifierRegistry.Instance.GetMultiplier(tags, ModifierType.SizeMultiplier)
            : 1f;
    }

    protected float GetLifeTimeMultiplier()
    {
        return ModifierRegistry.Instance != null
            ? ModifierRegistry.Instance.GetMultiplier(tags, ModifierType.LifeTimeMultiplier)
            : 1f;
    }

    protected int GetPierceBonus()
    {
        return ModifierRegistry.Instance != null
            ? ModifierRegistry.Instance.GetBonusInt(tags, ModifierType.PierceBonus)
            : 0;
    }
}