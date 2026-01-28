using UnityEngine;

namespace InGame
{
    public class PlayerController : MonoBehaviour
    {
        public Grid grid;
        public GameObject highlight;
        public MapCreator mapCreator;

        public Rigidbody2D rigid;

        public float speed = 5.0f;
        public Vector2 lastDirection;

        private void Awake()
        {
            rigid = GetComponent<Rigidbody2D>();
        }

        void Update()
        {
            float x = Input.GetAxisRaw("Horizontal");
            float y = Input.GetAxisRaw("Vertical");

            // 이동
            if (x != 0 || y != 0)
            {
                lastDirection = new Vector2(x, y).normalized;
                rigid.linearVelocity = lastDirection * speed;
            }
            else
            {
                rigid.linearVelocity = Vector2.zero;
            }

            // 하이라이트
            var targetCell = GetTargetCell();
            highlight.transform.position = grid.GetCellCenterWorld(targetCell);

            // 짓기
            if (Input.GetKeyDown(KeyCode.Alpha1))
                mapCreator.DrawObjectsMap(targetCell.x, targetCell.y, 3);
        }

        public Vector3Int GetTargetCell()
        {
            var origin = transform.position;
            var checkPos = origin + (Vector3)lastDirection * 0.5f;

            return grid.WorldToCell(checkPos);
        }
    }
}
