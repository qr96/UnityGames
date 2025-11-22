using GameUI;
using UnityEngine;

namespace InGame
{
    public class Managers : MonoBehaviour
    {
        public static Managers Instance;

        public static MonsterManager Monster => Instance?.monster;
        public static UIManager UI => Instance?.ui;

        MonsterManager monster;
        UIManager ui;

        private void Awake()
        {
            Instance = this;

            monster = GetComponent<MonsterManager>();
            ui = GetComponent<UIManager>();
        }
    }
}
