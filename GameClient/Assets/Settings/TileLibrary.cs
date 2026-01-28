using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "TileLibrary", menuName = "Game/Tile Library")]
public class TileLibrary : ScriptableObject
{
    // 배열이나 리스트를 사용하여 타일들을 한곳에 관리합니다.
    public TileBase[] tiles;

    // ID(인덱스)를 넣으면 해당 타일을 반환하는 함수
    public TileBase GetTile(int id)
    {
        if (id >= 0 && id < tiles.Length) return tiles[id];
        return null;
    }
}
