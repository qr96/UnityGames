using System;
using System.Collections;
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
        public Action<bool[]> OnChangeLock;
        public Action<List<BaseMagicScroll>> OnChangeScrollSlots;
        public Action<bool> OnChangeTurn; // true = player turn

        public Action<BaseUnit> OnSpawnPlayer;
        public Action<List<BaseUnit>> OnSpawnEnemies;
        public Action<BaseUnit> OnChangePlayerUnit;
        public Action<List<BaseUnit>> OnChangeEnemyUnit;

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
        bool isPlayerTurn = false;

        private void Start()
        {
            Initialize();
            CallAllActions();

            // 턴 체인지
            ChangeTurn();
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
            OnChangeElementSlots?.Invoke(elementSlots);
            OnChangeLock?.Invoke(lockedSlot);

            OnChangeScrollSlots(scrollSlots);
            
            OnSpawnPlayer?.Invoke(player);
            OnSpawnEnemies?.Invoke(enemyList);
            OnChangePlayerUnit?.Invoke(player);
            OnChangeEnemyUnit?.Invoke(enemyList);
        }

        void ChangeTurn()
        {
            isPlayerTurn = !isPlayerTurn;
            ResetSlots();
            OnChangeTurn?.Invoke(isPlayerTurn);
        }

        void ResetSlots()
        {
            remainRollCount = rollCount;
            for (int i = 0; i < Common.MaxSlotCount; i++)
            {
                elementSlots[i] = Common.ElementType.None;
                lockedSlot[i] = false;
            }

            OnChangeElementSlots?.Invoke(elementSlots);
            OnChangeLock?.Invoke(lockedSlot);
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

            OnChangeLock?.Invoke(lockedSlot);
        }

        // 랜덤 원소 생성
        public void RollDice()
        {
            // 롤은 플레이어 턴에만 가능
            if (!isPlayerTurn)
                return;

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

        public void UseScroll(int scrollIndex, BaseUnit target)
        {
            if (!isPlayerTurn)
                return;

            if (scrollIndex >= scrollSlots.Count)
                return;

            var scroll = scrollSlots[scrollIndex];
            if (scroll == null)
                return;

            if (scroll.GetTargetType() == Common.ScrollTargetType.Single)
            {
                scrollSlots[scrollIndex].Execute(elementSlots, player, target);
            }
            else if (scroll.GetTargetType() == Common.ScrollTargetType.All)
            {
                scrollSlots[scrollIndex].Execute(elementSlots, player, enemyList);
            }
            
            OnChangePlayerUnit?.Invoke(player);
            OnChangeEnemyUnit?.Invoke(enemyList);

            // 플레이어가 행동을 끝내면 턴 전환 및 적 턴 시작
            ChangeTurn();
            StartCoroutine(EnemyTurnRoutine());
        }

        IEnumerator EnemyTurnRoutine()
        {
            // 연출 딜레이
            yield return new WaitForSeconds(2f);

            // 플레이어 공격
            foreach (var enemy in enemyList)
            {
                if (enemy.nowStat.hp > 0)
                {
                    player.OnDamaged(enemy.nowStat.attack);
                    break;
                }
            }

            OnChangePlayerUnit?.Invoke(player);
            OnChangeEnemyUnit?.Invoke(enemyList);

            // 적 턴이 끝나면 다시 플레이어 턴으로 전환
            ChangeTurn();
        }
    }
}
