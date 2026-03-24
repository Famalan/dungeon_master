using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using System.Collections.Generic;

public class DungeonBuilder : MonoBehaviour
{
    [Header("References")]
    public DungeonGenerator generator;

    [Header("Tile Settings")]
    public float tileSize = 0.85f;
    public float wallHeight = 2.5f;

    [Header("Materials")]
    public Material floorMaterial;
    public Material wallMaterial;
    public Material ceilingMaterial;
    public Material exitMaterial;

    [Header("Prefabs")]
    public GameObject exitPortalPrefab;
    public GameObject torchPrefab;

    [Header("Corridor Lighting")]
    public int corridorLightInterval = 8;

    [Header("Loot")]
    public AudioClip chestOpenClip;

    Transform dungeonParent;
    List<Vector3> enemySpawnPoints = new List<Vector3>();
    List<int> enemySpawnRoomDistances = new List<int>();
    Vector3 playerSpawnPosition;
    Vector3 exitPosition;

    Material runtimeChestMaterial;

    public Vector3 PlayerSpawnPosition { get { return playerSpawnPosition; } }
    public Vector3 ExitPosition { get { return exitPosition; } }
    public List<Vector3> EnemySpawnPoints { get { return enemySpawnPoints; } }
    public List<int> EnemySpawnRoomDistances { get { return enemySpawnRoomDistances; } }

    public void BuildDungeon()
    {
        ClearDungeon();

        dungeonParent = new GameObject("Dungeon").transform;
        enemySpawnPoints.Clear();
        enemySpawnRoomDistances.Clear();

        BuildFloorAndWalls();
        PlaceSpecialObjects();
        PlaceRoomTorches();
        PlaceCorridorLights();
        PlaceLootChests();

        RebuildNavMesh();
    }

    public void ClearDungeon()
    {
        if (dungeonParent != null)
        {
            Destroy(dungeonParent.gameObject);
        }

        GameObject existing = GameObject.Find("Dungeon");
        if (existing != null)
        {
            Destroy(existing);
        }
    }

    void BuildFloorAndWalls()
    {
        bool[,] map = generator.FloorMap;
        int width = generator.gridWidth;
        int height = generator.gridHeight;

        GameObject floorParent = new GameObject("Floors");
        floorParent.transform.SetParent(dungeonParent);

        GameObject wallParent = new GameObject("Walls");
        wallParent.transform.SetParent(dungeonParent);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!map[x, y]) continue;

                GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.transform.SetParent(floorParent.transform);
                floor.transform.position = new Vector3(x * tileSize, 0, y * tileSize);
                floor.transform.localScale = new Vector3(tileSize, 0.1f, tileSize);
                floor.isStatic = true;

                if (floorMaterial != null)
                {
                    floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;
                }

                GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ceiling.transform.SetParent(floorParent.transform);
                ceiling.transform.position = new Vector3(x * tileSize, wallHeight, y * tileSize);
                ceiling.transform.localScale = new Vector3(tileSize, 0.1f, tileSize);
                ceiling.isStatic = true;

                Material ceilMat = ceilingMaterial != null ? ceilingMaterial : floorMaterial;
                if (ceilMat != null)
                {
                    ceiling.GetComponent<Renderer>().sharedMaterial = ceilMat;
                }

                bool hasNorthFloor = (y + 1 < height) && map[x, y + 1];
                bool hasSouthFloor = (y - 1 >= 0) && map[x, y - 1];
                bool hasEastFloor = (x + 1 < width) && map[x + 1, y];
                bool hasWestFloor = (x - 1 >= 0) && map[x - 1, y];

                if (!hasNorthFloor) CreateWall(wallParent.transform, x, y, Vector3.forward, wallMaterial);
                if (!hasSouthFloor) CreateWall(wallParent.transform, x, y, Vector3.back, wallMaterial);
                if (!hasEastFloor) CreateWall(wallParent.transform, x, y, Vector3.right, wallMaterial);
                if (!hasWestFloor) CreateWall(wallParent.transform, x, y, Vector3.left, wallMaterial);
            }
        }
    }

    void CreateWall(Transform parent, int x, int y, Vector3 direction, Material mat)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.SetParent(parent);
        wall.isStatic = true;

        float halfTile = tileSize * 0.5f;
        Vector3 basePos = new Vector3(x * tileSize, wallHeight * 0.5f, y * tileSize);
        Vector3 offset = direction * halfTile;

        wall.transform.position = basePos + offset;

        if (direction == Vector3.forward || direction == Vector3.back)
        {
            wall.transform.localScale = new Vector3(tileSize, wallHeight, 0.1f);
        }
        else
        {
            wall.transform.localScale = new Vector3(0.1f, wallHeight, tileSize);
        }

        if (mat != null)
        {
            wall.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }

    void PlaceSpecialObjects()
    {
        List<RoomData> rooms = generator.Rooms;

        for (int i = 0; i < rooms.Count; i++)
        {
            RoomData room = rooms[i];
            Vector3 roomCenter = new Vector3(room.Center.x * tileSize, 0.5f, room.Center.y * tileSize);

            if (room.Type == RoomType.Start)
            {
                playerSpawnPosition = roomCenter;
                continue;
            }

            if (room.Type == RoomType.Exit)
            {
                exitPosition = roomCenter;

                if (exitPortalPrefab != null)
                {
                    GameObject portal = Instantiate(exitPortalPrefab, roomCenter, Quaternion.identity, dungeonParent);
                    if (portal.GetComponent<ExitPortalTrigger>() == null)
                    {
                        portal.AddComponent<ExitPortalTrigger>();
                    }

                    AddExitBeaconIfMissing(portal);
                }
                else
                {
                    GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    marker.transform.SetParent(dungeonParent);
                    marker.transform.position = roomCenter;
                    marker.transform.localScale = new Vector3(2f, 0.1f, 2f);
                    marker.tag = "ExitPortal";

                    if (exitMaterial != null)
                    {
                        marker.GetComponent<Renderer>().sharedMaterial = exitMaterial;
                    }

                    if (marker.GetComponent<ExitPortalTrigger>() == null)
                    {
                        marker.AddComponent<ExitPortalTrigger>();
                    }

                    AddExitBeaconIfMissing(marker);
                }
            }

            int spawnCount = GetEnemyCountForRoom(room);
            for (int s = 0; s < spawnCount; s++)
            {
                float sx = Random.Range(room.X + 1, room.X + room.Width - 1) * tileSize;
                float sy = Random.Range(room.Y + 1, room.Y + room.Height - 1) * tileSize;
                enemySpawnPoints.Add(new Vector3(sx, 0.5f, sy));
                enemySpawnRoomDistances.Add(room.DistanceFromStart);
            }
        }
    }

    void AddExitBeaconIfMissing(GameObject exitRoot)
    {
        if (exitRoot == null) return;
        if (exitRoot.GetComponentInChildren<ExitBeaconPulse>(true) != null) return;

        GameObject glow = new GameObject("ExitGlow");
        glow.transform.SetParent(exitRoot.transform);
        glow.transform.localPosition = new Vector3(0f, 1.4f, 0f);

        Light point = glow.AddComponent<Light>();
        point.type = LightType.Point;
        // Тёплый акцент у выхода (spec): янтарно-зелёный, читается как «награда»
        point.color = new Color(1f, 0.92f, 0.42f);
        point.range = 14f;
        point.intensity = 2.65f;

        ExitBeaconPulse pulse = exitRoot.AddComponent<ExitBeaconPulse>();
        pulse.targetLight = point;
    }

    int GetEnemyCountForRoom(RoomData room)
    {
        if (room.Type == RoomType.Start) return 0;

        int dist = room.DistanceFromStart;

        if (room.Type == RoomType.Exit) return Random.Range(4, 7);
        if (dist < 15) return Random.Range(0, 2);
        if (dist < 30) return Random.Range(2, 4);
        return Random.Range(3, 6);
    }

    void PlaceRoomTorches()
    {
        bool[,] map = generator.FloorMap;
        int w = generator.gridWidth;
        int h = generator.gridHeight;
        List<RoomData> rooms = generator.Rooms;

        Vector3[] dirs = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };

        for (int i = 0; i < rooms.Count; i++)
        {
            RoomData room = rooms[i];

            Vector2Int[] corners =
            {
                new Vector2Int(room.X, room.Y),
                new Vector2Int(room.X + room.Width - 1, room.Y),
                new Vector2Int(room.X, room.Y + room.Height - 1),
                new Vector2Int(room.X + room.Width - 1, room.Y + room.Height - 1)
            };

            for (int c = 0; c < corners.Length; c++)
            {
                int cx = corners[c].x;
                int cy = corners[c].y;
                if (cx < 0 || cx >= w || cy < 0 || cy >= h || !map[cx, cy]) continue;

                for (int d = 0; d < dirs.Length; d++)
                {
                    if (HasWallInDirection(cx, cy, dirs[d], map, w, h))
                    {
                        SpawnWallTorch(cx, cy, dirs[d]);
                    }
                }
            }
        }
    }

    bool HasWallInDirection(int gx, int gy, Vector3 dir, bool[,] map, int w, int h)
    {
        if (gx < 0 || gx >= w || gy < 0 || gy >= h || !map[gx, gy]) return false;

        if (dir == Vector3.forward)
            return gy + 1 >= h || !map[gx, gy + 1];
        if (dir == Vector3.back)
            return gy - 1 < 0 || !map[gx, gy - 1];
        if (dir == Vector3.right)
            return gx + 1 >= w || !map[gx + 1, gy];
        if (dir == Vector3.left)
            return gx - 1 < 0 || !map[gx - 1, gy];
        return false;
    }

    Vector3 GetTorchWorldPosition(Vector3 cellCenterXZ, Vector3 wallNormal)
    {
        float halfTile = tileSize * 0.5f;
        float wallHalfThick = 0.05f;
        float pushIntoWall = 0.03f;
        float alongWall = halfTile - wallHalfThick + pushIntoWall;
        float floorTopY = 0.05f;
        float torchY = Mathf.Clamp(floorTopY + wallHeight * 0.48f, 1f, wallHeight - 0.15f);

        return new Vector3(
            cellCenterXZ.x + wallNormal.x * alongWall,
            torchY,
            cellCenterXZ.z + wallNormal.z * alongWall
        );
    }

    void SpawnWallTorch(int gridX, int gridY, Vector3 wallNormal)
    {
        Vector3 cellCenter = new Vector3(gridX * tileSize, 0f, gridY * tileSize);
        Vector3 pos = GetTorchWorldPosition(cellCenter, wallNormal);

        if (torchPrefab != null)
        {
            GameObject torch = Instantiate(torchPrefab, pos, Quaternion.identity, dungeonParent);
            RemoveAllColliders(torch);
        }
        else
        {
            CreateLightOnly(pos);
        }
    }

    void PlaceCorridorLights()
    {
        bool[,] map = generator.FloorMap;
        int width = generator.gridWidth;
        int height = generator.gridHeight;

        HashSet<long> placed = new HashSet<long>();
        int minSpacing = corridorLightInterval;

        List<CorridorData> corridors = generator.Corridors;
        for (int i = 0; i < corridors.Count; i++)
        {
            List<Vector2Int> cells = corridors[i].Cells;
            int sinceLastTorch = minSpacing;

            for (int c = 0; c < cells.Count; c++)
            {
                sinceLastTorch++;
                if (sinceLastTorch < minSpacing) continue;

                Vector2Int cell = cells[c];
                int mountX;
                int mountY;
                Vector3 wallDir = GetAdjacentWall(cell.x, cell.y, map, width, height);
                if (wallDir == Vector3.zero)
                {
                    if (!TryClosestPerimeterWall(cell.x, cell.y, map, width, height, out mountX, out mountY, out wallDir))
                        continue;
                }
                else
                {
                    mountX = cell.x;
                    mountY = cell.y;
                }

                long key = (long)cell.x * 10000 + cell.y;
                if (placed.Contains(key)) continue;
                placed.Add(key);

                Vector3 cellCenter = new Vector3(mountX * tileSize, 0f, mountY * tileSize);
                Vector3 pos = GetTorchWorldPosition(cellCenter, wallDir);

                if (torchPrefab != null)
                {
                    GameObject torch = Instantiate(torchPrefab, pos, Quaternion.identity, dungeonParent);
                    RemoveAllColliders(torch);
                }
                else
                {
                    CreateLightOnly(pos);
                }

                sinceLastTorch = 0;
            }
        }
    }

    Vector3 GetAdjacentWall(int x, int y, bool[,] map, int w, int h)
    {
        if (y + 1 >= h || !map[x, y + 1]) return Vector3.forward;
        if (y - 1 < 0 || !map[x, y - 1]) return Vector3.back;
        if (x + 1 >= w || !map[x + 1, y]) return Vector3.right;
        if (x - 1 < 0 || !map[x - 1, y]) return Vector3.left;
        return Vector3.zero;
    }

    bool TryClosestPerimeterWall(int cx, int cy, bool[,] map, int w, int h, out int gx, out int gy, out Vector3 wallNormal)
    {
        gx = cx;
        gy = cy;
        wallNormal = Vector3.zero;
        if (cx < 0 || cx >= w || cy < 0 || cy >= h || !map[cx, cy]) return false;

        int bestDist = int.MaxValue;
        int bestGx = cx;
        int bestGy = cy;
        Vector3 bestNormal = Vector3.zero;

        void TryWalk(int ddx, int ddy, Vector3 dir)
        {
            int x = cx;
            int y = cy;
            int dist = 0;
            while (true)
            {
                int nx = x + ddx;
                int ny = y + ddy;
                if (nx < 0 || nx >= w || ny < 0 || ny >= h || !map[nx, ny])
                {
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestGx = x;
                        bestGy = y;
                        bestNormal = dir;
                    }
                    return;
                }
                x = nx;
                y = ny;
                dist++;
            }
        }

        TryWalk(0, 1, Vector3.forward);
        TryWalk(0, -1, Vector3.back);
        TryWalk(1, 0, Vector3.right);
        TryWalk(-1, 0, Vector3.left);

        if (bestNormal == Vector3.zero) return false;
        gx = bestGx;
        gy = bestGy;
        wallNormal = bestNormal;
        return true;
    }

    void PlaceLootChests()
    {
        if (generator == null || generator.Rooms == null) return;

        List<int> candidates = new List<int>();
        for (int i = 0; i < generator.Rooms.Count; i++)
        {
            RoomData room = generator.Rooms[i];
            if (room.Type != RoomType.Normal) continue;
            if (room.Width < 4 || room.Height < 4) continue;
            candidates.Add(i);
        }

        if (candidates.Count == 0) return;

        int maxChests = Mathf.Min(3, candidates.Count);
        int want = Random.Range(1, maxChests + 1);

        for (int w = 0; w < want && candidates.Count > 0; w++)
        {
            int pick = Random.Range(0, candidates.Count);
            int roomIndex = candidates[pick];
            candidates.RemoveAt(pick);

            RoomData r = generator.Rooms[roomIndex];
            float gx = Random.Range(r.X + 1, r.X + r.Width - 1) * tileSize;
            float gz = Random.Range(r.Y + 1, r.Y + r.Height - 1) * tileSize;
            CreateLootChest(new Vector3(gx, 0f, gz));
        }
    }

    void CreateLootChest(Vector3 floorXZ)
    {
        float y = 0.05f + 0.22f;
        Vector3 pos = new Vector3(floorXZ.x, y, floorXZ.z);

        GameObject chest = GameObject.CreatePrimitive(PrimitiveType.Cube);
        chest.name = "LootChest";
        chest.layer = LayerMask.NameToLayer("Default");
        chest.transform.SetParent(dungeonParent);
        chest.transform.position = pos;
        chest.transform.localScale = new Vector3(0.5f, 0.44f, 0.38f);

        Renderer rend = chest.GetComponent<Renderer>();
        if (rend != null)
        {
            if (runtimeChestMaterial == null)
            {
                Shader lit = Shader.Find("Universal Render Pipeline/Lit");
                if (lit == null) lit = Shader.Find("Standard");
                runtimeChestMaterial = new Material(lit);
                runtimeChestMaterial.color = new Color(0.78f, 0.52f, 0.16f);
            }

            rend.sharedMaterial = runtimeChestMaterial;
        }

        Destroy(chest.GetComponent<Collider>());
        BoxCollider trigger = chest.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(1.2f, 1.05f, 1.2f);

        NavMeshObstacle obstacle = chest.AddComponent<NavMeshObstacle>();
        obstacle.size = new Vector3(0.52f, 0.44f, 0.4f);
        obstacle.carving = true;
        obstacle.carveOnlyStationary = true;

        LootChest loot = chest.AddComponent<LootChest>();
        loot.openSound = chestOpenClip;
    }

    void CreateLightOnly(Vector3 pos)
    {
        GameObject torchObj = new GameObject("WallLight");
        torchObj.transform.SetParent(dungeonParent);
        torchObj.transform.position = pos;

        Light pointLight = torchObj.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.range = 16f;
        pointLight.intensity = 3.5f;
        pointLight.color = new Color(1f, 0.8f, 0.45f);

        torchObj.AddComponent<DungeonTorch>();
    }

    void RemoveAllColliders(GameObject obj)
    {
        Collider[] colliders = obj.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Destroy(colliders[i]);
        }
    }

    void RebuildNavMesh()
    {
        NavMeshSurface surface = dungeonParent.GetComponent<NavMeshSurface>();
        if (surface == null)
        {
            surface = dungeonParent.gameObject.AddComponent<NavMeshSurface>();
        }
        surface.collectObjects = CollectObjects.Children;
        surface.BuildNavMesh();
    }
}
