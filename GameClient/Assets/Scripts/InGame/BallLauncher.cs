using UnityEngine;
using UnityEngine.EventSystems;

namespace InGame
{
    public class BallLauncher : MonoBehaviour
    {
        public GameObject guideLine;
        public Animator animator;

        public float launchPower = 500f;
        public float maxDragDistance = 2f;

        private Rigidbody rb;
        private Vector3 initialPosition; // 슈터 위치 (발사 시작 위치)
        private bool isDragging = false; // 현재 드래그 중인지 상태

        void Start()
        {
            rb = GetComponent<Rigidbody>();
        }

        void Update()
        {
            if (!rb.isKinematic)
            {
                if (rb.linearVelocity != Vector3.zero)
                    transform.forward = rb.linearVelocity.normalized;

                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                HandleMouseDown();
            }

            if (isDragging)
            {
                HandleMouseDrag();
            }

            if (Input.GetMouseButtonUp(0))
            {
                HandleMouseUp();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.transform.CompareTag("Enemy"))
            {
                if (PoolManager.Instance.TryCreate("Effects/HCFX_Hit_08", out var effect))
                {
                    effect.transform.position = collision.transform.position;
                    animator.SetTrigger("Attack");
                }
            }
        }

        private void HandleMouseDown()
        {
            // UI 위에 포인터가 있으면 입력을 무시 (UI 가로채기 방지)
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            isDragging = true;
            rb.isKinematic = true; // 만약을 위해 다시 Kinematic 설정
            initialPosition = transform.position;
        }

        private void HandleMouseDrag()
        {
            Vector3 launchDirection = GetLaunchVector();
            guideLine.transform.forward = launchDirection;
        }

        private void HandleMouseUp()
        {
            if (!isDragging) return;
            isDragging = false;

            Vector3 launchDirection = GetLaunchVector();
            if (launchDirection.magnitude < 0.1f)
                return;

            rb.isKinematic = false; // 물리 엔진 활성화
            rb.AddForce(launchDirection.normalized * launchDirection.magnitude * launchPower, ForceMode.Impulse);
            animator.SetBool("Move", true);

            guideLine.gameObject.SetActive(false);
        }

        Vector3 GetMouseWolrdPos()
        {
            Vector3 mouseScreenPos = Input.mousePosition;
            mouseScreenPos.z = Camera.main.nearClipPlane + 10f;
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

            return mouseWorldPos;
        }

        Vector3 GetDragVector()
        {
            Vector3 dragVector = GetMouseWolrdPos() - initialPosition;
            dragVector.y = 0f;

            return dragVector;
        }

        Vector3 GetLaunchVector()
        {
            Vector3 launchDirection = GetDragVector();

            if (launchDirection.magnitude > maxDragDistance)
                launchDirection = launchDirection.normalized * maxDragDistance;

            return launchDirection;
        }
    }
}
