using UnityEngine;

namespace InGame
{
    public class SpumAnimator : MonoBehaviour
    {
        public Animator animator;

        Rigidbody2D rb;

        // Settings
        float flipIgnoreTime = 0.1f;

        // Values
        float afterFlipTime;
        bool isFlipped; // 방향 전환 존재 여부
        float prevLinearVelX;
        State state;

        public enum State
        {
            Idle,
            Move,
            Attack
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            if (state == State.Move)
                OnUpdateMove();
        }

        public void SetState(State state)
        {
            this.state = state;

            if (state == State.Idle)
                OnStartIdle();
            else if (state == State.Attack)
                OnStartAttack();
        }

        void OnStartIdle()
        {
            animator.SetBool("1_Move", false);
        }

        void OnStartAttack()
        {
            animator.SetTrigger("2_Attack");
        }

        void OnUpdateMove()
        {
            if (rb != null)
            {
                // 캐릭터 방향 전환 감지
                if (prevLinearVelX * rb.linearVelocityX < 0)
                {
                    isFlipped = true;
                    afterFlipTime = 0f;
                }
                else
                {
                    if (isFlipped)
                        afterFlipTime += Time.deltaTime;
                }

                // 일정 시간 이상 같은 방향으로 이동 시 플립
                if (afterFlipTime > flipIgnoreTime)
                {
                    afterFlipTime = 0f;
                    isFlipped = false;

                    if (rb.linearVelocityX < 0)
                        animator.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                    else if (rb.linearVelocityX > 0)
                        animator.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                }

                animator.SetBool("1_Move", rb.linearVelocity != Vector2.zero);

                prevLinearVelX = rb.linearVelocityX;
            }
        }
    }
}
