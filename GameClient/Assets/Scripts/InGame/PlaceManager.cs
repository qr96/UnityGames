using GameUI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using static UnityEngine.UI.Image;

namespace InGame
{
    public class PlaceManager : MonoBehaviour
    {
        public MapCreator creator;
        public GameLayout layout;

        public Tilemap mainTilemap;

        public float dragSpeed;

        bool isDragging = false;
        Vector3 dragOrigin;

        void Update()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            isDragging = Input.GetMouseButton(0);

            if (layout.CurrentMode == GameLayout.Mode.MoveMode)
            {
                if (Input.GetMouseButtonDown(0))
                    dragOrigin = Camera.main.ScreenToWorldPoint(Input.mousePosition);

                if (isDragging)
                {
                    var difference = dragOrigin - Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    Camera.main.transform.position += new Vector3(difference.x, difference.y, 0f);
                }
            }
            else
            {
                if (isDragging)
                    RenderPreview();
            }
        }

        void RenderPreview()
        {
            // 마우스 위치를 월드 좌표로, 다시 타일 좌표(Int)로 변환
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int gridPos = mainTilemap.WorldToCell(mouseWorldPos);

            creator.DrawBlueprint(gridPos.x, gridPos.y, 2);
        }
    }
}
