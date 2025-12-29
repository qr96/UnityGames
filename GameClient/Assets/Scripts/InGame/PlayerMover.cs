using System;
using UnityEngine;

namespace InGame
{
    public class PlayerMover : MonoBehaviour
    {
        public static PlayerMover Instance;

        public Action<bool> OnChangeHoldMode;

        LeaderUnit unit;

        bool isHoldMode;
        bool isProduceUnitZone;

        void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        void Start()
        {
            unit = GetComponent<LeaderUnit>();

            isHoldMode = false;
            ToggleHoldMode();

            unit.OnZoneChanged += OnZoneChangeEvent;
        }

        void OnDestroy()
        {
            unit.OnZoneChanged -= OnZoneChangeEvent;
        }

        void Update()
        {
            var horizontalInput = Input.GetAxisRaw("Horizontal");
            var verticalInput = Input.GetAxisRaw("Vertical");
            var input = new Vector2(horizontalInput, verticalInput);

            if (unit != null)
                unit.SetInput(input);

            if (Input.GetKeyDown(KeyCode.Alpha3))
                ToggleHoldMode();

            if (isProduceUnitZone && Input.GetKeyDown(KeyCode.Alpha1))
                unit.ExecuteZoneEvent(1);
        }

        void ToggleHoldMode()
        {
            isHoldMode = !isHoldMode;
            unit.SetHoldMode(isHoldMode);
            OnChangeHoldMode?.Invoke(isHoldMode);
        }

        void OnZoneChangeEvent(IEventZone eventZone)
        {
            if (eventZone is ProduceUnitZone)
                isProduceUnitZone = true;
            else
                isProduceUnitZone = false;
        }
    }
}
