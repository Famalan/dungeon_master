using UnityEngine;
using System.Collections.Generic;

public class RoomSealManager : MonoBehaviour
{
    public Material sealMaterial;

    List<RoomSealZone> zones = new List<RoomSealZone>();

    public void SetupRoomSealing(DungeonGenerator generator, DungeonBuilder builder, Transform dungeonParent)
    {
        ClearZones();

        List<RoomData> rooms = generator.Rooms;
        bool[,] map = generator.FloorMap;
        int width = generator.gridWidth;
        int height = generator.gridHeight;

        for (int i = 0; i < rooms.Count; i++)
        {
            RoomData room = rooms[i];
            if (room.Type == RoomType.Start) continue;

            List<Vector2Int> doorways = FindDoorways(room, map, width, height);
            if (doorways.Count == 0) continue;

            GameObject zoneObj = new GameObject("RoomSealZone_" + i);
            zoneObj.transform.SetParent(dungeonParent);

            Vector3 center = new Vector3(room.Center.x * builder.tileSize, 1.5f, room.Center.y * builder.tileSize);
            float sizeX = (room.Width - 2) * builder.tileSize;
            float sizeZ = (room.Height - 2) * builder.tileSize;
            if (sizeX < 1f) sizeX = 1f;
            if (sizeZ < 1f) sizeZ = 1f;

            BoxCollider trigger = zoneObj.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = center;
            trigger.size = new Vector3(sizeX, 4f, sizeZ);

            Rigidbody rb = zoneObj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            RoomSealZone zone = zoneObj.AddComponent<RoomSealZone>();
            zone.sealMaterial = sealMaterial;
            zone.tileSize = builder.tileSize;
            zone.wallHeight = builder.wallHeight;

            for (int d = 0; d < doorways.Count; d++)
            {
                zone.doorwayPositions.Add(doorways[d]);
            }

            zones.Add(zone);
        }
    }

    List<Vector2Int> FindDoorways(RoomData room, bool[,] map, int width, int height)
    {
        List<Vector2Int> doorways = new List<Vector2Int>();

        for (int x = room.X; x < room.X + room.Width; x++)
        {
            if (room.Y - 1 >= 0 && map[x, room.Y - 1])
                AddUnique(doorways, new Vector2Int(x, room.Y));
            if (room.Y + room.Height < height && map[x, room.Y + room.Height])
                AddUnique(doorways, new Vector2Int(x, room.Y + room.Height - 1));
        }

        for (int y = room.Y; y < room.Y + room.Height; y++)
        {
            if (room.X - 1 >= 0 && map[room.X - 1, y])
                AddUnique(doorways, new Vector2Int(room.X, y));
            if (room.X + room.Width < width && map[room.X + room.Width, y])
                AddUnique(doorways, new Vector2Int(room.X + room.Width - 1, y));
        }

        return doorways;
    }

    void AddUnique(List<Vector2Int> list, Vector2Int item)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].x == item.x && list[i].y == item.y) return;
        }
        list.Add(item);
    }

    void ClearZones()
    {
        for (int i = 0; i < zones.Count; i++)
        {
            if (zones[i] != null)
            {
                Destroy(zones[i].gameObject);
            }
        }
        zones.Clear();
    }
}
