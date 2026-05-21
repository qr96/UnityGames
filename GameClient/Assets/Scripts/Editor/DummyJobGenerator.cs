#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using AutoBattler.Core;
using AutoBattler.Data;

namespace AutoBattler.EditorTools
{
    /// <summary>
    /// 메뉴: AutoBattler > Generate > Dummy Jobs
    /// 현재는 초보자(Job_Novice)만 생성. 직업 추가하려면 이 파일에 항목 추가.
    /// 위치: Assets/_Generated/Jobs
    /// </summary>
    public static class DummyJobGenerator
    {
        private const string JobDir = "Assets/_Generated/Jobs";

        [MenuItem("AutoBattler/Generate/Dummy Jobs")]
        public static void Generate()
        {
            EnsureFolder(JobDir);

            MakeJob("Job_Novice", "초보자",
                description: "방망이를 들고 휘두르는 견습생.",
                statBonus: Stats.Zero,           // 보너스 없음, 영웅 기본 그대로
                motion: AttackMotion.Swing);

            // 추후 추가할 직업 예시 (주석 풀어서 사용):
            //
            // MakeJob("Job_Warrior", "전사",
            //     description: "검을 휘두르는 강한 근접 전사.",
            //     statBonus: new Stats { attack = 4, maxHp = 20 },
            //     motion: AttackMotion.Swing);
            //
            // MakeJob("Job_Archer", "궁수",
            //     description: "활로 멀리서 적을 공격.",
            //     statBonus: new Stats { attackRange = 3, attackSpeed = 20 },
            //     motion: AttackMotion.Shoot);
            //
            // MakeJob("Job_Mage", "마법사",
            //     description: "지팡이로 마법을 시전.",
            //     statBonus: new Stats { attackRange = 2, attack = 4 },
            //     motion: AttackMotion.Cast);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DummyJobGenerator] 직업 생성 완료 → " + JobDir);
        }

        private static JobData MakeJob(string fileName, string display,
            string description, Stats statBonus, AttackMotion motion)
        {
            string path = $"{JobDir}/{fileName}.asset";
            var j = AssetDatabase.LoadAssetAtPath<JobData>(path);
            if (j == null)
            {
                j = ScriptableObject.CreateInstance<JobData>();
                AssetDatabase.CreateAsset(j, path);
            }
            j.id = fileName;
            j.displayName = display;
            j.description = description;
            j.statBonus = statBonus;
            j.attackMotion = motion;
            EditorUtility.SetDirty(j);
            return j;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string[] parts = path.Split('/');
            string cur = parts[0];
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
