using UnityEngine;
using UnityEngine.EventSystems;

namespace InGame
{
    public class BallLauncher : MonoBehaviour
    {
        public GameObject guideLine;
        public GameObject guideLine2;

        public GameObject ballPrefab;

        public float launchPower = 10f;
        public int ballCount;

        Rigidbody2D rb;
        Vector3 launchPosition; // 슈터 위치 (발사 시작 위치)
        bool isDragging = false; // 현재 드래그 중인지 상태

        void Start()
        {
            rb = ballPrefab.GetComponent<Rigidbody2D>();
        }

        void Update()
        {
            if (rb.bodyType == RigidbodyType2D.Dynamic)
            {
                var ballTransform = ballPrefab.transform;

                if (ballTransform.position.y < -2.8f)
                {
                    launchPosition = new Vector3(ballTransform.position.x, -2.8f, ballTransform.position.z);
                    ballTransform.position = launchPosition;
                    rb.bodyType = RigidbodyType2D.Kinematic;
                    rb.linearVelocity = Vector2.zero;
                }
                else
                {
                    if (rb.linearVelocity != Vector2.zero)
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
                }
            }
        }

        private void HandleMouseDown()
        {
            // UI 위에 포인터가 있으면 입력을 무시 (UI 가로채기 방지)
            if (EventSystem.current.IsPointerOverGameObject())
                return;

            var ballTransform = ballPrefab.transform;

            isDragging = true;
            rb.bodyType = RigidbodyType2D.Kinematic; // 만약을 위해 다시 Kinematic 설정
            launchPosition = ballTransform.position;

            // 가이드 라인 활성화
            guideLine.SetActive(true);
            guideLine.transform.position = transform.position;
        }

        private void HandleMouseDrag()
        {
            Vector3 launchDirection = GetLaunchVector();
            guideLine.transform.forward = launchDirection;

            if (Physics.Raycast(launchPosition, launchDirection, out var hit, 26f, LayerMask.GetMask("Wall")))
            {
                var wallNormal = hit.normal;
                var reflect = Vector3.Reflect(launchDirection, wallNormal);
                var distance = Vector3.Magnitude(launchPosition - hit.point);
                var newDis = 26f - distance;
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

            rb.bodyType = RigidbodyType2D.Dynamic; // 물리 엔진 활성화
            rb.AddForce(launchDirection.normalized * launchPower, ForceMode2D.Impulse);

            guideLine.SetActive(false);
            guideLine2.SetActive(false);
        }

        Vector3 GetMouseWolrdPos()
        {
            Vector3 mouseScreenPos = Input.mousePosition;
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

            return mouseWorldPos;
        }

        Vector3 GetDragVector()
        {
            Vector3 dragVector = GetMouseWolrdPos() - launchPosition;
            dragVector.z = 0f;

            return dragVector;
        }

        Vector3 GetLaunchVector()
        {
            Vector3 launchDirection = GetDragVector();
            return launchDirection;
        }
    }
}
