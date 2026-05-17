using System;
using System.Collections.Generic;
using UnityEngine;

namespace AutoBattler.Data
{
    /// <summary>
    /// 라운드별 인카운터 풀.
    /// RunManager가 라운드 시작 시 GetCandidatesFor(round)로 후보를 받고 그 중 랜덤 추첨.
    ///
    /// 예시:
    ///   round=1: [Encounter_GoblinTrio, Encounter_GoblinDuo]
    ///   round=2: [Encounter_GoblinTrio, Encounter_Wolf]
    ///   ...
    /// </summary>
    [CreateAssetMenu(menuName = "AutoBattler/EncounterTable", fileName = "EncounterTable")]
    public class EncounterTable : ScriptableObject
    {
        public List<RoundEntry> rounds = new List<RoundEntry>();

        /// <summary>지정 라운드에서 추첨 가능한 인카운터 목록.</summary>
        public List<EncounterData> GetCandidatesFor(int round)
        {
            var list = new List<EncounterData>();
            foreach (var r in rounds)
            {
                if (r.round != round) continue;
                if (r.candidates != null) list.AddRange(r.candidates);
            }
            return list;
        }
    }

    [Serializable]
    public class RoundEntry
    {
        public int round;
        public EncounterData[] candidates;
    }
}
