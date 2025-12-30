using InGame;
using System.Collections.Generic;
using UnityEngine;

namespace InGame
{
    public class MinionFormationCommander : MonoBehaviour
    {
        public Vector2 gap;
        public int maxColumn;

        // Values
        List<SoldierUnit> minions = new List<SoldierUnit>();
        Dictionary<SoldierUnit, bool> commanded = new Dictionary<SoldierUnit, bool>();

        bool isHoldMode;

        public void SetMinionsPosition(Vector2 leaderPos, Vector3 leaderDir)
        {
            if (minions.Count == 0)
                return;

            // 열 수보다 병력 적은 경우 처리. (병력 수에 따라 열 조정)
            var nowColumn = minions.Count + 1 > 9 ? 5 : 3;
            if (nowColumn > 15) nowColumn = Mathf.Max(nowColumn, maxColumn);

            // 아래 보고 정렬함.
            var pivot = new Vector2(-gap.x * (nowColumn - 1) / 2f, 0f);

            // 명령 받은 여부 초기화
            foreach (var unit in minions)
                commanded[unit] = false;

            for (int i = 0; i < minions.Count + 1; i++)
            {
                var idx = new Vector2(i % nowColumn, i / nowColumn);

                // 리더의 자리.
                if (idx.x == nowColumn / 2 && idx.y == 0)
                    continue;

                var relativePos = new Vector2(idx.x * gap.x, idx.y * gap.y) + pivot;
                var rotation = Quaternion.FromToRotation(Vector2.down, leaderDir);
                var rotatedPos = rotation * relativePos;
                var absPos = (Vector2)rotatedPos + leaderPos;

                // 해당 위치에 가장 가까운 유닛 찾아 이동시킴.
                var closeUnit = FindCloseUnit(absPos);
                if (closeUnit != null)
                {
                    closeUnit.SetLeaderMoving(false);
                    closeUnit.MoveCommand(absPos);
                    commanded[closeUnit] = true;
                }
                else
                {
                    Debug.LogError("unit can't be null.");
                }
            }
        }

        public void AddMinion(SoldierUnit minion)
        {
            minion.SetCommander(this);
            minions.Add(minion);
            commanded.Add(minion, false);
            minion.SetHoldMode(isHoldMode);
        }

        public void RemoveMinion(SoldierUnit minion)
        {
            if (minions.Contains(minion))
                minions.Remove(minion);
            else
                Debug.LogError($"Failed to find key in minions. minion={minion.name}");

            if (commanded.ContainsKey(minion))
                commanded.Remove(minion);
            else
                Debug.LogError($"Failed to find key in commanded. minion={minion.name}");
        }

        public void ReleaseFormation()
        {
            foreach (var unit in minions)
                unit.SetLeaderMoving(true);
        }

        public void SetHoldMode(bool holding)
        {
            isHoldMode = holding;

            foreach (var unit in minions)
                unit.SetHoldMode(holding);
        }

        SoldierUnit FindCloseUnit(Vector2 position)
        {
            if (minions.Count == 0)
                return null;

            SoldierUnit closeUnit = null;
            float closeDis = float.MaxValue;

            for (int i = 0; i < minions.Count; i++)
            {
                if (!commanded[minions[i]])
                {
                    var dis = (position - (Vector2)minions[i].transform.position).sqrMagnitude;
                    if (dis < closeDis)
                    {
                        closeUnit = minions[i];
                        closeDis = dis;
                    }
                }
            }

            return closeUnit;
        }
    }
}

