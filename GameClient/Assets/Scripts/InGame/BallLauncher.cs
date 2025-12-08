using UnityEngine;
using UnityEngine.EventSystems;

namespace InGame
{
    public class BallLauncher : MonoBehaviour
    {
        public GameObject guideLine;
        public GameObject guideLine2;
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
                if (transform.position.z < -7f)
                {
                    initialPosition = new Vector3(transform.position.x, transform.position.y, -7f);
                    transform.position = initialPosition;
                    rb.isKinematic = true;
                    transform.forward = Vector3.forward;
                    animator.SetBool("Move", false);
                }
                else
                {
                    if (rb.linearVelocity != Vector3.zero)
                        transform.forward = rb.linearVelocity.normalized;
                }

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
                var enemy = collision.transform.GetComponent<Enemy>();
                if (enemy != null && PoolManager.Instance.TryCreate("Effects/HCFX_Hit_08", out var effect))
                {
                    enemy.OnDamaged(5);
                    effect.transform.position = collision.transform.position + new Vector3(0f, 0.5f, 0f);
                    animator.SetTrigger("Attack");
                }
            }
        }

        private void HandleMouseDown()
        {
            // UI 위에 포인터가 있으면 입력을 무시 (UI 가로채기 방지)
            if (EventSystem.current.IsPointerOverGameObject())
                return;

            isDragging = true;
            rb.isKinematic = true; // 만약을 위해 다시 Kinematic 설정
            initialPosition = new Vector3(transform.position.x, transform.position.y, -7f);

            // 가이드 라인 활성화
            guideLine.SetActive(true);
            guideLine.transform.position = transform.position;
        }

        private void HandleMouseDrag()
        {
            Vector3 launchDirection = GetLaunchVector();
            guideLine.transform.forward = launchDirection;

            if (Physics.Raycast(initialPosition, launchDirection, out var hit, 20f, LayerMask.GetMask("Wall")))
            {
                var wallNormal = hit.normal;
                var reflect = Vector3.Reflect(launchDirection, wallNormal);
                var distance = Vector3.Magnitude(initialPosition - hit.point);
                var newDis = 16f - distance;
                var detailLine = guideLine2.transform.GetChild(0);

                if (newDis > 0f)
                {
                    detailLine.transform.localScale = new Vector3(detailLine.transform.localScale.x, detailLine.transform.localScale.y, newDis);
                    detailLine.transform.localPosition = new Vector3(detailLine.transform.localPosition.x, detailLine.transform.localPosition.y, newDis / 2f - 0.5f);

                    guideLine2.SetActive(true);
                    guideLine2.transform.position = hit.point;
                    guideLine2.transform.forward = reflect;
                }
                else
                {
                    guideLine2.SetActive(false);
                }
            }
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

            guideLine.SetActive(false);
            guideLine2.SetActive(false);
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
