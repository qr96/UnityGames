using UnityEngine;

namespace InGame
{
    public class LeaderUnit : MonoBehaviour
    {
        // Settings
        public float moveSpeed = 5f;

        // Values
        public int TeamId;

        Rigidbody2D rb;
        MinionFormationCommander formationCommander;
        SpumAnimator animator;

        bool alreadyStop;
        Vector2 input;
        Vector2 lastDir;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            formationCommander = GetComponent<MinionFormationCommander>();
            animator = GetComponent<SpumAnimator>();
        }

        void Update()
        {
            rb.linearVelocity = input * moveSpeed;

            if (input != Vector2.zero)
            {
                if (alreadyStop)
                {
                    alreadyStop = false;
                    formationCommander.ReleaseFormation();
                    animator.SetState(SpumAnimator.State.Move);
                }
            }
            else
            {
                if (!alreadyStop)
                {
                    alreadyStop = true;
                    formationCommander.SetMinionsPosition(rb.position, lastDir);
                    animator.SetState(SpumAnimator.State.Idle);
                }
            }
        }

        public void SetInput(Vector2 dir)
        {
            input = dir.normalized;

            if (input != Vector2.zero)
                lastDir = input;
        }

        public void SetHoldMode(bool holding)
        {
            Debug.Log(holding);
            formationCommander.SetHoldMode(holding);
        }
    }
}
