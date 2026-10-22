using UnityEngine;
using UnityEngine.Tilemaps;

public class InfiniteHexTilemap : MonoBehaviour
{
    public Tilemap tilemap;
    public TileBase tile;
    public Transform player;

    public int radius = 20;

    private Vector3Int lastCenter;

    void Start()
    {
        UpdateTiles(true);
    }

    void Update()
    {
        Vector3Int playerCell = tilemap.WorldToCell(player.position);

        if (playerCell != lastCenter)
        {
            lastCenter = playerCell;
            UpdateTiles(false);
        }
    }

    void UpdateTiles(bool force)
    {
        tilemap.ClearAllTiles();

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector3Int pos = new Vector3Int(
                    lastCenter.x + x,
                    lastCenter.y + y,
                    0
                );

                tilemap.SetTile(pos, tile);
            }
        }
    }
}