using UnityEngine;
using System.Collections.Generic;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridWidth = 80;
    public int gridHeight = 80;

    [Header("Room Settings")]
    public int maxRooms = 14;
    public int roomMinSize = 4;
    public int roomMaxSize = 7;
    public int roomPadding = 2;

    [Header("Generation")]
    public int maxPlacementAttempts = 100;

    public List<RoomData> Rooms { get; private set; }
    public List<CorridorData> Corridors { get; private set; }
    public bool[,] FloorMap { get; private set; }

    public void Generate()
    {
        for (int gen = 0; gen < 20; gen++)
        {
            Rooms = new List<RoomData>();
            Corridors = new List<CorridorData>();
            FloorMap = new bool[gridWidth, gridHeight];

            PlaceRooms();

            if (Rooms.Count < 4) continue;

            AssignRoomTypes();

            RoomData startRoom = null;
            RoomData exitRoom = null;
            for (int i = 0; i < Rooms.Count; i++)
            {
                if (Rooms[i].Type == RoomType.Start) startRoom = Rooms[i];
                if (Rooms[i].Type == RoomType.Exit) exitRoom = Rooms[i];
            }

            if (startRoom == null || exitRoom == null) continue;

            int dist = Mathf.Abs(startRoom.Center.x - exitRoom.Center.x) +
                       Mathf.Abs(startRoom.Center.y - exitRoom.Center.y);

            if (dist < gridWidth / 3) continue;

            CreateCorridors();
            ComputeDistances();
            BakeFloorMap();
            return;
        }

        CreateCorridors();
        ComputeDistances();
        BakeFloorMap();
    }

    void PlaceRooms()
    {
        for (int i = 0; i < maxRooms; i++)
        {
            bool placed = false;

            for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
            {
                int w = Random.Range(roomMinSize, roomMaxSize + 1);
                int h = Random.Range(roomMinSize, roomMaxSize + 1);
                int x = Random.Range(1, gridWidth - w - 1);
                int y = Random.Range(1, gridHeight - h - 1);

                RoomData newRoom = new RoomData();
                newRoom.X = x;
                newRoom.Y = y;
                newRoom.Width = w;
                newRoom.Height = h;

                bool overlaps = false;
                for (int j = 0; j < Rooms.Count; j++)
                {
                    if (newRoom.Overlaps(Rooms[j], roomPadding))
                    {
                        overlaps = true;
                        break;
                    }
                }

                if (!overlaps)
                {
                    Rooms.Add(newRoom);
                    placed = true;
                    break;
                }
            }

            if (!placed) break;
        }
    }

    void AssignRoomTypes()
    {
        if (Rooms.Count == 0) return;

        int closestToOriginIdx = 0;
        float closestDist = float.MaxValue;

        for (int i = 0; i < Rooms.Count; i++)
        {
            Rooms[i].Type = RoomType.Normal;
            float d = Rooms[i].Center.magnitude;
            if (d < closestDist)
            {
                closestDist = d;
                closestToOriginIdx = i;
            }
        }

        Rooms[closestToOriginIdx].Type = RoomType.Start;

        int farthestIdx = 0;
        int maxManhattan = 0;
        Vector2Int startCenter = Rooms[closestToOriginIdx].Center;

        for (int i = 0; i < Rooms.Count; i++)
        {
            if (i == closestToOriginIdx) continue;

            int manhattan = Mathf.Abs(Rooms[i].Center.x - startCenter.x) +
                            Mathf.Abs(Rooms[i].Center.y - startCenter.y);

            if (manhattan > maxManhattan)
            {
                maxManhattan = manhattan;
                farthestIdx = i;
            }
        }

        Rooms[farthestIdx].Type = RoomType.Exit;
    }

    void ComputeDistances()
    {
        Vector2Int startCenter = Vector2Int.zero;
        for (int i = 0; i < Rooms.Count; i++)
        {
            if (Rooms[i].Type == RoomType.Start)
            {
                startCenter = Rooms[i].Center;
                break;
            }
        }

        for (int i = 0; i < Rooms.Count; i++)
        {
            Rooms[i].DistanceFromStart = Mathf.Abs(Rooms[i].Center.x - startCenter.x) +
                                          Mathf.Abs(Rooms[i].Center.y - startCenter.y);
        }
    }

    void CreateCorridors()
    {
        for (int i = 0; i < Rooms.Count - 1; i++)
        {
            Vector2Int start = Rooms[i].Center;
            Vector2Int end = Rooms[i + 1].Center;

            Rooms[i].ConnectedRoomIndexes.Add(i + 1);
            Rooms[i + 1].ConnectedRoomIndexes.Add(i);

            CorridorData corridor = new CorridorData();

            int x = start.x;
            int dirX = (end.x > start.x) ? 1 : -1;
            while (x != end.x)
            {
                corridor.Cells.Add(new Vector2Int(x, start.y));
                x += dirX;
            }

            int y = start.y;
            int dirY = (end.y > start.y) ? 1 : -1;
            while (y != end.y)
            {
                corridor.Cells.Add(new Vector2Int(end.x, y));
                y += dirY;
            }
            corridor.Cells.Add(new Vector2Int(end.x, end.y));

            Corridors.Add(corridor);
        }
    }

    void BakeFloorMap()
    {
        for (int i = 0; i < Rooms.Count; i++)
        {
            RoomData room = Rooms[i];
            for (int x = room.X; x < room.X + room.Width; x++)
            {
                for (int y = room.Y; y < room.Y + room.Height; y++)
                {
                    if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
                    {
                        FloorMap[x, y] = true;
                    }
                }
            }
        }

        for (int i = 0; i < Corridors.Count; i++)
        {
            CorridorData corridor = Corridors[i];
            for (int c = 0; c < corridor.Cells.Count; c++)
            {
                Vector2Int cell = corridor.Cells[c];
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int nx = cell.x + dx;
                        int ny = cell.y + dy;
                        if (nx >= 0 && nx < gridWidth && ny >= 0 && ny < gridHeight)
                        {
                            FloorMap[nx, ny] = true;
                        }
                    }
                }
            }
        }
    }
}
