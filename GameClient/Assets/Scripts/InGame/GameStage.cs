using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

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
        public Action<List<BaseMagicScroll>> OnChangeScrollSlots;
        public Action<BaseUnit> OnChangePlayerUnit;
        public Action<BaseUnit> OnChageEnemyUnit;

        // 슬롯
        int rollCount = 3;
        int remainRollCount = 3;
        bool[] lockedSlot = new bool[Common.MaxSlotCount];
        Common.ElementType[] elementSlots = new Common.ElementType[Common.MaxSlotCount];

        // 스크롤
        int maxScrollCount = 10;
        List<BaseMagicScroll> scrollSlots = new List<BaseMagicScroll>();
        HashSet<int> usedScrollIndexes = new HashSet<int>();

        // 유닛
        BaseUnit player;
        List<BaseUnit> enemyList = new List<BaseUnit>();

        private void Start()
        {
            Initialize();
            CallAllActions();
        }

        void Initialize()
        {
            scrollSlots.Add(new BasicFireScroll());
            scrollSlots.Add(new BasicIceScroll());

            player = new BaseUnit(new Stat() { hp = 100, attack = 10 });
            enemyList.Add(new BaseUnit(new Stat() { hp = 100, attack = 10 }));
            player.Spawn();
            foreach (var enemy in enemyList)
                enemy.Spawn();
        }

        // 모든 콜백 호출
        void CallAllActions()
        {
            for (int i = 0; i < Common.MaxSlotCount; i++)
            {
                OnChangeElementSlots?.Invoke(elementSlots);
                OnChangeLock?.Invoke(i, lockedSlot[i]);
            }

            OnChangeScrollSlots(scrollSlots);
            OnChangePlayerUnit?.Invoke(player);
            OnChageEnemyUnit?.Invoke(enemyList[0]);
        }

        void ChangeTurn()
        {
            remainRollCount = rollCount;
        }

        // 슬롯 잠금 상태 변환
        public void ChangeSlotLock(int index)
        {
            // 슬롯 인덱스 체크
            if (index < 0 || index >= Common.MaxSlotCount)
                return;

            // 슬롯 None이면 잠금 불가
            if (elementSlots[index] == Common.ElementType.None)
                return;

            lockedSlot[index] = !lockedSlot[index];

            OnChangeLock?.Invoke(index, lockedSlot[index]);
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

        // 원소 롤
        public void UseScroll(int scrollIndex)
        {
            if (scrollIndex >= scrollSlots.Count)
                return;

            scrollSlots[scrollIndex].Execute(elementSlots, player, enemyList);

            OnChangePlayerUnit?.Invoke(player);
            OnChageEnemyUnit?.Invoke(enemyList[0]);
        }
    }
}
