using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InGame
{
    public class PlayerMover : MonoBehaviour
    {
        public Joystick joystick;

        public float moveSpeed = 5f;

        private Rigidbody2D rb;

        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        void FixedUpdate()
        {
            var horizontalInput = Input.GetAxisRaw("Horizontal");
            var verticalInput = Input.GetAxisRaw("Vertical");
            var input = new Vector2(horizontalInput, verticalInput);

            input = input.normalized * moveSpeed;// * Time.fixedDeltaTime;
            rb.linearVelocity = input;
        }
    }
}
