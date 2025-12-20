using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InGame
{
    public class PlayerMover : MonoBehaviour
    {
        public Joystick joystick;

        public float moveSpeed = 5f;

        Rigidbody2D rb;
        MinionFormationCommander formationCommander;
        bool alreadyStop;
        Vector2 lastDir;

        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            formationCommander = GetComponent<MinionFormationCommander>();
        }

        void FixedUpdate()
        {
            var horizontalInput = Input.GetAxisRaw("Horizontal");
            var verticalInput = Input.GetAxisRaw("Vertical");
            var input = new Vector2(horizontalInput, verticalInput);

            input = input.normalized * moveSpeed;// * Time.fixedDeltaTime;
            rb.linearVelocity = input;

            if (input != Vector2.zero)
            {
                if (alreadyStop)
                {
                    alreadyStop = false;
                    formationCommander.ReleaseFormation();
                }

                lastDir = input;
            }
            else
            {
                if (!alreadyStop)
                {
                    formationCommander.SetMinionsPosition(rb.position, lastDir);
                    alreadyStop = true;
                }
            }
        }
    }
}
