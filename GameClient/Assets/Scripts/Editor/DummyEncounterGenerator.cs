#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using AutoBattler.Core;
using AutoBattler.Data;

namespace AutoBattler.EditorTools
{
    /// <summary>
    /// 메뉴: AutoBattler > Generate > Dummy Encounters
    /// 생성: 적 3종 + 인카운터 5종 + 테이블 1개
    /// 위치: Assets/_Generated/Enemies, Encounters
    /// </summary>
    public static class DummyEncounterGenerator
    {
        private const string EnemyDir = "Assets/_Generated/Enemies";
        private const string EncounterDir = "Assets/_Generated/Encounters";

        [MenuItem("AutoBattler/Generate/Dummy Encounters")]
        public static void Generate()
        {
            EnsureFolder(EnemyDir);
            EnsureFolder(EncounterDir);

            // 적 3종
            var goblin = MakeEnemy("Enemy_Goblin", "Goblin",
                atk: 6, def: 1, hp: 40, atkSpd: 100, attackRange: 0, moveSpd: 1.2f);

            var orc = MakeEnemy("Enemy_Orc", "Orc",
                atk: 10, def: 2, hp: 80, atkSpd: 80, attackRange: 0, moveSpd: 0.9f);

            var archer = MakeEnemy("Enemy_Archer", "Archer",
                atk: 8, def: 1, hp: 50, atkSpd: 100, attackRange: 3, moveSpd: 1f);

            // 인카운터 5종
            var trio = MakeEncounter("Encounter_GoblinTrio", "고블린 셋",
                (goblin, new Vector2Int(1, 8)),
                (goblin, new Vector2Int(2, 8)),
                (goblin, new Vector2Int(3, 8)));

            var orcDuo = MakeEncounter("Encounter_OrcDuo", "오크 둘",
                (orc, new Vector2Int(1, 9)),
                (orc, new Vector2Int(3, 9)));

            var mixed = MakeEncounter("Encounter_Mixed", "혼성 부대",
                (goblin, new Vector2Int(0, 7)),
                (orc, new Vector2Int(2, 9)),
                (archer, new Vector2Int(4, 8)));

            var ambush = MakeEncounter("Encounter_Ambush", "양면 공격",
                (goblin, new Vector2Int(0, 6)),
                (goblin, new Vector2Int(4, 6)),
                (archer, new Vector2Int(2, 9)));

            var boss = MakeEncounter("Encounter_OrcKing", "오크 왕",
                (orc, new Vector2Int(2, 9)),
                (orc, new Vector2Int(1, 8)),
                (orc, new Vector2Int(3, 8)),
                (archer, new Vector2Int(0, 9)),
                (archer, new Vector2Int(4, 9)));

            // 테이블
            var table = ScriptableObject.CreateInstance<EncounterTable>();
            string tablePath = $"{EncounterDir}/EncounterTable.asset";
            var existing = AssetDatabase.LoadAssetAtPath<EncounterTable>(tablePath);
            if (existing != null) AssetDatabase.DeleteAsset(tablePath);
            AssetDatabase.CreateAsset(table, tablePath);

            // 모든 라운드(1~15)에 대해 후보 풀 채움
            table.rounds.Clear();
            for (int r = 1; r <= 15; r++)
            {
                EncounterData[] pool;
                if (r >= 14) pool = new[] { boss };                              // 보스 라운드
                else if (r >= 10) pool = new[] { mixed, ambush, orcDuo };
                else if (r >= 5) pool = new[] { trio, orcDuo, mixed };
                else pool = new[] { trio, orcDuo };
                table.rounds.Add(new RoundEntry { round = r, candidates = pool });
            }
            EditorUtility.SetDirty(table);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DummyEncounterGenerator] 적 3종 + 인카운터 5종 + 테이블 1개 생성 완료.");
        }

        private static EnemyData MakeEnemy(string fileName, string display,
            float atk, float def, float hp,
            float atkSpd, int attackRange, float moveSpd)
        {
            string path = $"{EnemyDir}/{fileName}.asset";
            var e = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (e == null)
            {
                e = ScriptableObject.CreateInstance<EnemyData>();
                AssetDatabase.CreateAsset(e, path);
            }
            e.id = fileName;
            e.displayName = display;
            e.baseStats = new Stats
            {
                attack = atk,
                defense = def,
                maxHp = hp,
                critRate = 0.05f,
                critDamage = 1.5f,
                attackSpeed = atkSpd,
                attackRange = attackRange,
                moveSpeed = moveSpd
            };
            EditorUtility.SetDirty(e);
            return e;
        }

        private static EncounterData MakeEncounter(string fileName, string display,
            params (EnemyData enemy, Vector2Int cell)[] placements)
        {
            string path = $"{EncounterDir}/{fileName}.asset";
            var enc = AssetDatabase.LoadAssetAtPath<EncounterData>(path);
            if (enc == null)
            {
                enc = ScriptableObject.CreateInstance<EncounterData>();
                AssetDatabase.CreateAsset(enc, path);
            }
            enc.id = fileName;
            enc.displayName = display;
            enc.enemies.Clear();
            foreach (var p in placements)
                enc.enemies.Add(new EncounterEnemy { enemyData = p.enemy, cell = p.cell });
            EditorUtility.SetDirty(enc);
            return enc;
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