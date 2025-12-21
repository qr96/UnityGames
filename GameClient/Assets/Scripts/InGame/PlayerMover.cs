using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InGame
{
    public class PlayerMover : MonoBehaviour
    {
        LeaderUnit unit;

        void Start()
        {
            unit = GetComponent<LeaderUnit>();
        }

        void Update()
        {
            var horizontalInput = Input.GetAxisRaw("Horizontal");
            var verticalInput = Input.GetAxisRaw("Vertical");
            var input = new Vector2(horizontalInput, verticalInput);

            if (unit != null)
                unit.SetInput(input);

            if (Input.GetKeyDown(KeyCode.Alpha1))
                unit.SetRegroup();
        }
    }
}
