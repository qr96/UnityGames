using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public class GameLayout : MonoBehaviour
    {
        public Button modeToggle;
        public GameObject moveMode;
        public GameObject buildMode;

        public Mode CurrentMode { get; private set; }

        public enum Mode
        {
            MoveMode,
            BuildMode,
        }

        private void Start()
        {
            modeToggle.onClick.AddListener(ChangeMode);
            SetMode(Mode.MoveMode);
        }

        void ChangeMode()
        {
            CurrentMode = CurrentMode == Mode.MoveMode ? Mode.BuildMode : Mode.MoveMode;
            SetMode(CurrentMode);
        }

        void SetMode(Mode mode)
        {
            moveMode.SetActive(mode == Mode.MoveMode);
            buildMode.SetActive(mode == Mode.BuildMode);
        }
    }
}
