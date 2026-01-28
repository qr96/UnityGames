using UnityEngine;
using UnityEngine.Tilemaps;

public class MapCreator : MonoBehaviour
{
    public Tilemap groundTileMap;
    public Tilemap objectTileMap;
    public Tilemap blueprintTileMap;
    public TileBase[] tilePresets;

    enum TileLayer
    {
        None = 0,
        Ground = 1,
        Objects = 2,
    }

    private void Start()
    {
        int[,] mapData = new int[24, 24];
        
        DrawGroundMap(mapData);
        DrawObjectsMap(3, 4, 1);
        DrawObjectsMap(3, 5, 1);
        DrawObjectsMap(3, 6, 1);
        DrawObjectsMap(5, 4, 1);
    }

    public void DrawGroundMap(int[,] mapData)
    {
        for (int x = 0; x < mapData.GetLength(0); x++)
        {
            for (int y = 0; y < mapData.GetLength(1); y++)
            {
                int tileType = mapData[x, y];
                DrawGroundMap(x, y, tileType);
            }
        }
    }

    public void DrawBlueprint(int x, int y, int tileType)
    {
        DrawTileMap(x, y, blueprintTileMap, tilePresets[tileType]);
    }

    public void DrawObjectsMap(int x, int y, int tileType)
    {
        if (objectTileMap.GetTile(new Vector3Int(x, y, 0)) == null)
            DrawTileMap(x, y, objectTileMap, tilePresets[tileType]);
        else
            DrawTileMap(x, y, objectTileMap, null);
    }

    void DrawGroundMap(int x, int y, int tileType)
    {
        DrawTileMap(x, y, groundTileMap, tilePresets[tileType]);
    }

    void DrawTileMap(int x, int y, Tilemap tilemap, TileBase tileBase)
    {
        tilemap.SetTile(new Vector3Int(x, y, 0), tileBase);
    }
}
