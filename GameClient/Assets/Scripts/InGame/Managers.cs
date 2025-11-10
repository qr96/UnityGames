using UnityEngine;

namespace InGame
{
    public class Managers : MonoBehaviour
    {
        public static Managers Instance;

        private void Awake()
        {
            Instance = this;
        }
    }
}
