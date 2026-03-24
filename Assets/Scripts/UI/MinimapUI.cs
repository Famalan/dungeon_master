using UnityEngine;
using System.Collections.Generic;

public class MinimapUI : MonoBehaviour
{
    [Header("Minimap Settings")]
    public int mapSize = 150;
    public int cellSize = 2;
    public float padding = 10f;

    [Header("Colors")]
    public Color floorColor = new Color(0.3f, 0.3f, 0.4f, 0.8f);
    public Color wallColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
    public Color playerColor = Color.green;
    public Color exitColor = Color.yellow;
    public Color backgroundColor = new Color(0, 0, 0, 0.5f);

    [Header("References")]
    public DungeonGenerator dungeonGenerator;
    public Transform playerTransform;

    [Tooltip("Must match DungeonBuilder.tileSize — world size of one grid cell.")]
    public float worldTileSize = 0.85f;

    Texture2D mapTexture;
    bool mapReady;

    public void GenerateMapTexture()
    {
        if (dungeonGenerator == null || dungeonGenerator.FloorMap == null) return;

        bool[,] map = dungeonGenerator.FloorMap;
        int width = map.GetLength(0);
        int height = map.GetLength(1);

        mapTexture = new Texture2D(width, height);
        mapTexture.filterMode = FilterMode.Point;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (map[x, y])
                {
                    mapTexture.SetPixel(x, y, floorColor);
                }
                else
                {
                    mapTexture.SetPixel(x, y, Color.clear);
                }
            }
        }

        // Mark exit room
        List<RoomData> rooms = dungeonGenerator.Rooms;
        for (int i = 0; i < rooms.Count; i++)
        {
            if (rooms[i].Type == RoomType.Exit)
            {
                Vector2Int center = rooms[i].Center;
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int px = center.x + dx;
                        int py = center.y + dy;
                        if (px >= 0 && px < width && py >= 0 && py < height)
                        {
                            mapTexture.SetPixel(px, py, exitColor);
                        }
                    }
                }
            }
        }

        mapTexture.Apply();
        mapReady = true;
    }

    void OnGUI()
    {
        if (!mapReady || mapTexture == null) return;

        float drawX = Screen.width - mapSize - padding;
        float drawY = Screen.height - mapSize - padding;

        // Background
        GUI.color = backgroundColor;
        GUI.DrawTexture(new Rect(drawX - 2, drawY - 2, mapSize + 4, mapSize + 4), Texture2D.whiteTexture);

        // Map
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(drawX, drawY, mapSize, mapSize), mapTexture);

        // Player dot
        if (playerTransform != null && dungeonGenerator != null)
        {
            float worldW = dungeonGenerator.gridWidth * worldTileSize;
            float worldH = dungeonGenerator.gridHeight * worldTileSize;
            if (worldW < 0.001f) worldW = 0.001f;
            if (worldH < 0.001f) worldH = 0.001f;

            float relX = playerTransform.position.x / worldW;
            float relY = playerTransform.position.z / worldH;

            float dotX = drawX + relX * mapSize;
            float dotY = drawY + (1f - relY) * mapSize;

            GUI.color = playerColor;
            GUI.DrawTexture(new Rect(dotX - 3, dotY - 3, 6, 6), Texture2D.whiteTexture);
        }
    }
}
