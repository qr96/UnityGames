#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using AutoBattler.Core;
using AutoBattler.Data;

namespace AutoBattler.EditorTools
{
    /// <summary>
    /// 테스트용 SkillData 자동 생성.
    /// 메뉴: AutoBattler > Generate > Dummy Skills
    ///
    /// 생성 위치: Assets/_Generated/Skills/
    /// - 기본 스킬 3종 (1단계)
    /// - 각각의 합성 결과 (2단계)
    /// 1단계 SkillData.upgradedVersion 이 2단계를 가리키도록 자동 연결.
    ///
    /// 타깃팅은 3축 (TeamFilter / RangeFilter / Selector) 기반.
    /// </summary>
    public static class DummySkillGenerator
    {
        private const string OutDir = "Assets/_Generated/Skills";

        [MenuItem("AutoBattler/Generate/Dummy Skills")]
        public static void Generate()
        {
            EnsureFolder(OutDir);

            // 2단계 (합성 결과)부터 만들어야 1단계가 참조할 수 있음
            var fireballPlus = MakeSkill("Skill_Fireball+", "Fireball+",
                cooldown: 4f, range: 4, dmgMul: 4f, splash: 1,
                team: SkillTeamFilter.Enemy, selector: SkillTargetSelector.Nearest,
                upgraded: null);

            var healPlus = MakeSkill("Skill_Heal+", "Heal+",
                cooldown: 5f, range: 0, dmgMul: 0f, healAmt: 60f,
                team: SkillTeamFilter.Ally, selector: SkillTargetSelector.LowestHP,
                upgraded: null);

            var slashPlus = MakeSkill("Skill_Slash+", "Slash+",
                cooldown: 3f, range: 1, dmgMul: 3f,
                team: SkillTeamFilter.Enemy, selector: SkillTargetSelector.Nearest,
                upgraded: null);

            // 1단계
            MakeSkill("Skill_Fireball", "Fireball",
                cooldown: 5f, range: 4, dmgMul: 2.5f, splash: 1,
                team: SkillTeamFilter.Enemy, selector: SkillTargetSelector.Nearest,
                upgraded: fireballPlus);

            MakeSkill("Skill_Heal", "Heal",
                cooldown: 6f, range: 0, dmgMul: 0f, healAmt: 30f,
                team: SkillTeamFilter.Ally, selector: SkillTargetSelector.LowestHP,
                upgraded: healPlus);

            MakeSkill("Skill_Slash", "Slash",
                cooldown: 4f, range: 1, dmgMul: 1.8f,
                team: SkillTeamFilter.Enemy, selector: SkillTargetSelector.Nearest,
                upgraded: slashPlus);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DummySkillGenerator] 6개 SkillData 생성 완료 → " + OutDir);
        }

        private static SkillData MakeSkill(
            string fileName, string display,
            float cooldown, int range, float dmgMul,
            float healAmt = 0f,
            int splash = 0,
            SkillTeamFilter team = SkillTeamFilter.Enemy,
            SkillRangeFilter rangeFilter = SkillRangeFilter.WithinSkillRange,
            SkillTargetSelector selector = SkillTargetSelector.Nearest,
            SkillData upgraded = null)
        {
            string path = $"{OutDir}/{fileName}.asset";
            var s = AssetDatabase.LoadAssetAtPath<SkillData>(path);
            if (s == null)
            {
                s = ScriptableObject.CreateInstance<SkillData>();
                AssetDatabase.CreateAsset(s, path);
            }

            s.id = fileName;
            s.displayName = display;
            s.cooldown = cooldown;
            s.range = range;
            s.damageMultiplier = dmgMul;
            s.healAmount = healAmt;
            s.splashRadius = splash;
            s.teamFilter = team;
            s.rangeFilter = rangeFilter;
            s.selector = selector;
            s.upgradedVersion = upgraded;

            EditorUtility.SetDirty(s);
            return s;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string[] parts = path.Split('/');
            string cur = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }
}
#endif