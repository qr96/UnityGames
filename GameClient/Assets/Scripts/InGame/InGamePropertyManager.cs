using System;
using System.Collections;
using UnityEngine;

namespace InGame
{
    public class InGamePropertyManager : MonoBehaviour
    {
        public static InGamePropertyManager Instance;

        public long produceFoodPerSec;
        public long currentFood;

        public Action<long> OnChangeFoodEvent;

        Coroutine produceFoodCo;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(Instance);
        }

        private void Start()
        {
            if (produceFoodCo != null)
                StopCoroutine(produceFoodCo);

            produceFoodCo = StartCoroutine(ProduceFoodCo());
        }

        public void AddFood(int teamId, long food)
        {
            SetFood(currentFood + food);
        }

        public bool UseFood(int teamId, long food)
        {
            if (currentFood < food)
                return false;

            SetFood(currentFood - food);
            return true;
        }

        IEnumerator ProduceFoodCo()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                AddFood(1, produceFoodPerSec);
            }
        }

        void SetFood(long food)
        {
            currentFood = food;
            OnChangeFoodEvent?.Invoke(currentFood);
        }
    }
}
