using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class DungeonPreviewWindow : EditorWindow
{
    int gridWidth = 80;
    int gridHeight = 80;
    int maxRooms = 10;
    int roomMinSize = 5;
    int roomMaxSize = 12;
    int roomPadding = 2;

    bool[,] previewMap;
    List<RoomData> previewRooms;
    int previewCorridorCount;
    Vector2 scrollPos;

    [MenuItem("Tools/Dungeon Preview")]
    public static void ShowWindow()
    {
        GetWindow<DungeonPreviewWindow>("Dungeon Preview");
    }

    void OnGUI()
    {
        GUILayout.Label("Dungeon Generation Preview", EditorStyles.boldLabel);
        GUILayout.Space(10);

        gridWidth = EditorGUILayout.IntSlider("Grid Width", gridWidth, 30, 150);
        gridHeight = EditorGUILayout.IntSlider("Grid Height", gridHeight, 30, 150);
        maxRooms = EditorGUILayout.IntSlider("Max Rooms", maxRooms, 3, 20);
        roomMinSize = EditorGUILayout.IntSlider("Room Min Size", roomMinSize, 3, 10);
        roomMaxSize = EditorGUILayout.IntSlider("Room Max Size", roomMaxSize, 6, 20);
        roomPadding = EditorGUILayout.IntSlider("Room Padding", roomPadding, 0, 5);

        GUILayout.Space(10);

        if (GUILayout.Button("Generate Preview", GUILayout.Height(30)))
        {
            GeneratePreview();
        }

        GUILayout.Space(10);

        if (previewMap != null && previewRooms != null)
        {
            GUILayout.Label("Rooms: " + previewRooms.Count + " | Corridors: " + previewCorridorCount);
            GUILayout.Space(5);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            DrawPreviewMap();
            EditorGUILayout.EndScrollView();
        }
    }

    void GeneratePreview()
    {
        GameObject tempObj = new GameObject("TempDungeonGen");
        DungeonGenerator gen = tempObj.AddComponent<DungeonGenerator>();
        gen.gridWidth = gridWidth;
        gen.gridHeight = gridHeight;
        gen.maxRooms = maxRooms;
        gen.roomMinSize = roomMinSize;
        gen.roomMaxSize = roomMaxSize;
        gen.roomPadding = roomPadding;

        gen.Generate();

        previewMap = gen.FloorMap;
        previewRooms = new List<RoomData>(gen.Rooms);
        previewCorridorCount = gen.Corridors.Count;

        DestroyImmediate(tempObj);

        Repaint();
    }

    void DrawPreviewMap()
    {
        if (previewMap == null) return;

        int cellSize = 4;
        int width = previewMap.GetLength(0);
        int height = previewMap.GetLength(1);

        Rect totalRect = GUILayoutUtility.GetRect(width * cellSize, height * cellSize);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!previewMap[x, y]) continue;

                Rect cellRect = new Rect(
                    totalRect.x + x * cellSize,
                    totalRect.y + (height - 1 - y) * cellSize,
                    cellSize,
                    cellSize
                );

                Color cellColor = Color.gray;

                for (int r = 0; r < previewRooms.Count; r++)
                {
                    RoomData room = previewRooms[r];
                    if (x >= room.X && x < room.X + room.Width
                        && y >= room.Y && y < room.Y + room.Height)
                    {
                        if (room.Type == RoomType.Start)
                            cellColor = Color.green;
                        else if (room.Type == RoomType.Exit)
                            cellColor = Color.red;
                        else
                            cellColor = new Color(0.4f, 0.4f, 0.6f);
                        break;
                    }
                }

                EditorGUI.DrawRect(cellRect, cellColor);
            }
        }
    }
}
