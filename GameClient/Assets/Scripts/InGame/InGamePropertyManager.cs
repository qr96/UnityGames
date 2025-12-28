using System;
using System.Collections;
using UnityEngine;

namespace InGame
{
    public class InGamePropertyManager
    {
        public long currentFood;
        public long foodProduction;

        public Action<long> OnChangeFoodEvent;

        public InGamePropertyManager(long currentFood)
        {
            SetFood(currentFood);
        }

        public void ProduceFood()
        {
            SetFood(currentFood + foodProduction);
        }

        public void AddFood(long food)
        {
            SetFood(currentFood + food);
        }

        public bool TryUseFood(long food)
        {
            if (currentFood < food)
                return false;

            SetFood(currentFood - food);
            return true;
        }

        void SetFood(long food)
        {
            currentFood = food;
            OnChangeFoodEvent?.Invoke(currentFood);
        }
    }
}
