using System;
using System.Collections.Generic;
using AutoBattler.Data;

namespace AutoBattler.Heroes
{
    /// <summary>
    /// 스킬 인벤토리. 같은 스킬을 여러 개 보관할 수 있고,
    /// 합성(3개 → 1개 상위)도 여기서 처리.
    ///
    /// RunManager 가 보유하고 UI 가 읽음. UI 는 OnChanged 구독해서 갱신.
    /// </summary>
    [Serializable]
    public class SkillInventory
    {
        // SkillData → 보유 수
        private readonly Dictionary<SkillData, int> _counts = new Dictionary<SkillData, int>();

        public event Action OnChanged;

        public IReadOnlyDictionary<SkillData, int> Counts => _counts;

        public int CountOf(SkillData s) =>
            (s != null && _counts.TryGetValue(s, out var c)) ? c : 0;

        public void Add(SkillData s, int n = 1)
        {
            if (s == null || n <= 0) return;
            _counts.TryGetValue(s, out int cur);
            _counts[s] = cur + n;
            OnChanged?.Invoke();
        }

        public bool Remove(SkillData s, int n = 1)
        {
            if (s == null || n <= 0) return false;
            if (!_counts.TryGetValue(s, out int cur) || cur < n) return false;
            cur -= n;
            if (cur <= 0) _counts.Remove(s);
            else _counts[s] = cur;
            OnChanged?.Invoke();
            return true;
        }

        public void Clear()
        {
            _counts.Clear();
            OnChanged?.Invoke();
        }

        /// <summary>해당 스킬을 합성 가능한가? (3개 이상 + upgradedVersion 존재)</summary>
        public bool CanFuse(SkillData s) =>
            s != null && s.upgradedVersion != null && CountOf(s) >= 3;

        /// <summary>3개 소비하고 upgradedVersion 1개 추가.</summary>
        public bool TryFuse(SkillData s)
        {
            if (!CanFuse(s)) return false;
            Remove(s, 3);
            Add(s.upgradedVersion, 1);
            // (이미 Add/Remove 가 OnChanged 호출하지만 한번 더 보내도 무방)
            return true;
        }
    }
}
