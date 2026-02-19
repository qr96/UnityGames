using GameDefine;
using System.Collections.Generic;
using UnityEngine;

namespace InGame
{
    public class GameField : MonoBehaviour
    {
        public static GameField Instance;

        public SkillCardManager cardManager;

        List<BaseUnit> enemies = new List<BaseUnit>();
        BaseUnit player;

        private void Awake()
        {
            if (Instance != null)
                Destroy(this);
            else
                Instance = this;

            cardManager = GetComponent<SkillCardManager>();
        }

        public void UseCard()
        {

        }
    }
}
