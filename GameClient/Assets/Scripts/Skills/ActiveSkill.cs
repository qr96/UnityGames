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

    private float timer = 0f;

    public bool CanLevelUp => level < maxLevel;

    public void Tick(float deltaTime, Vector3 playerPosition)
    {
        timer += deltaTime;

        // 모디파이어 적용한 최종 쿨다운
        float cdMul = ModifierRegistry.Instance != null
            ? ModifierRegistry.Instance.GetMultiplier(tags, ModifierType.CooldownMultiplier)
            : 1f;
        float finalCooldown = Mathf.Max(0.05f, baseCooldown * cdMul);

        if (timer >= finalCooldown)
        {
            timer = 0f;
            Execute(playerPosition);
        }
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

    // ─── 헬퍼: 자식 클래스가 발사 시 사용 ───

    /// <summary>모디파이어 + 레벨 보너스가 적용된 최종 데미지.</summary>
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