using UnityEngine;
using GameUI;

namespace InGame
{
    public class Managers : MonoBehaviour
    {
        static Managers Instance;

        public static UIManager UI => Instance?.ui;

        UIManager ui;

        private void Awake()
        {
            Instance = this;

            ui = GetComponent<UIManager>();
        }
    }
}

