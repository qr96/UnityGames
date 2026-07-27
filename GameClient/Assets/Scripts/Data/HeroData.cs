using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 영웅 정의 데이터. Project 창에서 우클릭 → Create → Game → Hero Data 로 생성.
/// 능력치는 전부 여기(데이터)에만 존재하고 코드에 하드코딩하지 않는다.
/// 이후 구글시트 → SO 임포트 파이프라인의 대상이 되는 클래스.
/// </summary>
[CreateAssetMenu(fileName = "Hero_", menuName = "Game/Hero Data")]
public class HeroData : ScriptableObject
{
    [Header("식별")]
    public string heroId;
    public string displayName;
    public Sprite portrait;

    [Header("기본 능력치 (1성 기준)")]
    public int attack = 10;              // 공격력
    [Range(0f, 1f)] public float critChance = 0.05f;  // 치확
    public float critMultiplier = 1.5f;  // 치피
    public int maxEnergy = 3;            // 기력 (스테이지당 발사 가능 횟수)

    [Header("투사체")]
    public float projectileSpeed = 18f;
    public float projectileRadius = 0.15f;
    public Color projectileColor = Color.white;

    [Header("고유 효과 (해금 성급 + 효과 SO)")]
    [Tooltip("성급이 unlockStar 이상일 때만 활성. 룬도 같은 EffectDefinition 타입을 사용")]
    public List<HeroEffectSlot> effects = new List<HeroEffectSlot>();

    /// <summary>현재 성급에서 활성화되는 효과를 buffer에 수집 (발사 시점에 호출).</summary>
    public void CollectActiveEffects(int star, List<EffectDefinition> buffer)
    {
        for (int i = 0; i < effects.Count; i++)
        {
            var slot = effects[i];
            if (slot.effect != null && star >= slot.unlockStar)
                buffer.Add(slot.effect);
        }
    }

    /// <summary>치명타 판정 포함 최종 데미지 계산.</summary>
    public int RollDamage(System.Random rng, out bool isCrit)
    {
        isCrit = rng.NextDouble() < critChance;
        float dmg = isCrit ? attack * critMultiplier : attack;
        return Mathf.Max(1, Mathf.RoundToInt(dmg));
    }
}

/// <summary>영웅 고유 효과 슬롯: 효과 + 해금 성급.</summary>
[System.Serializable]
public class HeroEffectSlot
{
    public EffectDefinition effect;
    [Range(1, 6), Tooltip("이 성급 이상일 때 활성화 (1 = 처음부터)")]
    public int unlockStar = 1;
}