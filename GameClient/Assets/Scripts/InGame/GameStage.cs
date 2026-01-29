using System;
using System.Collections.Generic;
using UnityEngine;

namespace InGame
{
    public class GameStage : MonoBehaviour
    {
        public static GameStage Instance
        {
            get
            {
                if (_instance == null)
                {
                    // 씬에서 기존 객체 찾기
                    _instance = FindFirstObjectByType<GameStage>();

                    // 씬에 없다면 새로 생성
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GameStage");
                        _instance = go.AddComponent<GameStage>();

                        // 씬이 넘어가도 파괴되지 않게 설정
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        private static GameStage _instance;

        public Action<Common.ElementType[]> OnChangeElementSlots;
        public Action<int, bool> OnChangeLock;

        // 슬롯
        int remainRollCount = 3;
        bool[] lockedSlot = new bool[Common.MaxSlotCount];
        Common.ElementType[] elementSlots = new Common.ElementType[Common.MaxSlotCount];

        // 스크롤
        int maxScrollCount = 10;
        List<Common.ScrollType> scrollSlots = new List<Common.ScrollType>();
        HashSet<int> usedScrollIndexes = new HashSet<int>();

        // 유닛
        Unit player;
        Unit enemy;

        private void Start()
        {
            Initialize();
            CallAllActions();
        }

        void Initialize()
        {
            scrollSlots.Add(Common.ScrollType.StandardFire);
            scrollSlots.Add(Common.ScrollType.StandardIce);
            scrollSlots.Add(Common.ScrollType.StandardEarth);

            player = new Unit(new Stat() { hp = 100, attack = 10 });
            enemy = new Unit(new Stat() { hp = 100, attack = 10 });
        }

        // 모든 콜백 호출
        void CallAllActions()
        {
            for (int i = 0; i < Common.MaxSlotCount; i++)
            {
                OnChangeElementSlots?.Invoke(elementSlots);
                OnChangeLock?.Invoke(i, lockedSlot[i]);
            }
        }

        // 슬롯 잠금 상태 변환
        public void ChangeSlotLock(int slot)
        {
            // 슬롯 인덱스 체크
            if (slot < 0 || slot >= Common.MaxSlotCount)
                return;

            lockedSlot[slot] = !lockedSlot[slot];

            OnChangeLock?.Invoke(slot, lockedSlot[slot]);
        }

        // 랜덤 원소 생성
        public void RollDice()
        {
            if (remainRollCount < 1)
                return;

            remainRollCount--;

            for (int i = 0; i < Common.MaxSlotCount; i++)
            {
                if (lockedSlot[i])
                    continue;

                var rand = UnityEngine.Random.Range(1, Common.ElementCount + 1);
                elementSlots[i] = (Common.ElementType)rand;
            }

            OnChangeElementSlots?.Invoke(elementSlots);
        }

        public void UseScroll(int scrollIndex)
        {
            if (scrollIndex >= scrollSlots.Count)
                return;

            if (usedScrollIndexes.Contains(scrollIndex))
                return;

            usedScrollIndexes.Add(scrollIndex);

            var scrollType = scrollSlots[scrollIndex];
            UseStandardFireScroll();
        }

        void UseStandardFireScroll()
        {
            var fireCount = GameUtil.GetElementCount(elementSlots, Common.ElementType.Fire);
            var damage = player.nowStat.attack * fireCount;

            enemy.OnDamaged(damage);
            Debug.Log($"[GameStage] hp:{enemy.nowStat.hp}");
        }
    }
}
