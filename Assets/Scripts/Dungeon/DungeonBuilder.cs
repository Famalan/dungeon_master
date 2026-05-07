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
    public float wallThickness = 0.1f;
    public float wallVisualOverlap = 0.08f;
    public float wallCollisionOverlap = 0.16f;

    [Header("Materials")]
    public Material floorMaterial;
    public Material wallMaterial;
    public Material ceilingMaterial;
    public Material exitMaterial;

    [Header("Prefabs")]
    public GameObject exitPortalPrefab;
    public GameObject torchPrefab;
    public GameObject[] wallPrefabs;

    [Header("Corridor Lighting")]
    public int corridorLightInterval = 8;

    [Header("Decoration")]
    public DungeonDecorLayer decorLayer;
    public int enemySafeRoomSteps = 1;
    public float minEnemySpawnDistanceFromPlayer = 10f;

    [Header("Loot")]
    public GameObject lootChestPrefab;
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
        PlaceDecorLayer();
    }

    void PlaceDecorLayer()
    {
        if (decorLayer == null)
        {
            decorLayer = GetComponent<DungeonDecorLayer>();
        }

        if (decorLayer != null)
        {
            decorLayer.Decorate(dungeonParent, generator, tileSize, wallHeight, CreatePlacementContext());
        }
    }

    DungeonPlacementContext CreatePlacementContext()
    {
        DungeonPlacementContext context = new DungeonPlacementContext();
        context.ReserveCircle(playerSpawnPosition, 2.6f);
        context.ReserveCircle(exitPosition, 2.1f);

        for (int i = 0; i < enemySpawnPoints.Count; i++)
        {
            context.ReserveCircle(enemySpawnPoints[i], 1.1f);
        }

        if (dungeonParent != null)
        {
            LootChest[] chests = dungeonParent.GetComponentsInChildren<LootChest>(true);
            for (int i = 0; i < chests.Length; i++)
            {
                Bounds bounds;
                if (TryGetRendererBounds(chests[i].gameObject, out bounds))
                {
                    context.ReserveCircle(bounds.center, Mathf.Max(bounds.extents.x, bounds.extents.z) + 0.35f);
                }
                else
                {
                    context.ReserveCircle(chests[i].transform.position, 1.1f);
                }
            }
        }

        return context;
    }

    public void ClearDungeon()
    {
        if (dungeonParent != null)
        {
            DestroyDungeonObject(dungeonParent.gameObject);
        }

        GameObject existing = GameObject.Find("Dungeon");
        if (existing != null)
        {
            DestroyDungeonObject(existing);
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
                ceiling.transform.position = new Vector3(x * tileSize, wallHeight + 0.025f, y * tileSize);
                float ceilingOverlap = Mathf.Max(0.03f, wallThickness * 0.8f);
                ceiling.transform.localScale = new Vector3(tileSize + ceilingOverlap, 0.14f, tileSize + ceilingOverlap);
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
        float halfTile = tileSize * 0.5f;
        Vector3 basePos = new Vector3(x * tileSize, wallHeight * 0.5f, y * tileSize);
        Vector3 offset = direction * halfTile;
        Vector3 wallCenter = basePos + offset;

        GameObject wallPrefab = GetWallPrefab();
        if (wallPrefab != null)
        {
            GameObject wallInstance = Instantiate(wallPrefab, Vector3.zero, GetWallMountedRotation(direction), parent);
            wallInstance.name = wallPrefab.name;
            ScaleWallPrefabToTile(wallInstance, wallPrefab);
            AlignRendererBoundsCenter(wallInstance, wallCenter);
            RemoveAllColliders(wallInstance);
            AddWallBoxCollider(wallInstance, wallCenter);
            SetStaticRecursively(wallInstance);
            return;
        }

        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.SetParent(parent);
        wall.isStatic = true;

        wall.transform.position = wallCenter;

        float thickness = Mathf.Max(0.01f, wallThickness);
        if (direction == Vector3.forward || direction == Vector3.back)
        {
            wall.transform.localScale = new Vector3(GetWallVisualLength(), wallHeight, thickness);
        }
        else
        {
            wall.transform.localScale = new Vector3(thickness, wallHeight, GetWallVisualLength());
        }

        if (mat != null)
        {
            wall.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }

    GameObject GetWallPrefab()
    {
        if (wallPrefabs == null || wallPrefabs.Length == 0) return null;

        int start = Random.Range(0, wallPrefabs.Length);
        for (int i = 0; i < wallPrefabs.Length; i++)
        {
            GameObject prefab = wallPrefabs[(start + i) % wallPrefabs.Length];
            if (prefab != null) return prefab;
        }

        return null;
    }

    Quaternion GetWallMountedRotation(Vector3 wallNormal)
    {
        Vector3 interiorDirection = -wallNormal;
        if (interiorDirection.sqrMagnitude < 0.001f)
        {
            return Quaternion.identity;
        }

        return Quaternion.LookRotation(interiorDirection, Vector3.up);
    }

    void ScaleWallPrefabToTile(GameObject wallInstance, GameObject sourcePrefab)
    {
        Bounds sourceBounds;
        if (!TryGetRendererBounds(sourcePrefab, out sourceBounds)) return;

        Vector3 sourceSize = sourceBounds.size;
        Vector3 scale = wallInstance.transform.localScale;
        scale.x *= GetWallVisualLength() / Mathf.Max(0.001f, sourceSize.x);
        scale.y *= wallHeight / Mathf.Max(0.001f, sourceSize.y);
        scale.z *= Mathf.Max(0.01f, wallThickness) / Mathf.Max(0.001f, sourceSize.z);
        wallInstance.transform.localScale = scale;
    }

    float GetWallVisualLength()
    {
        return tileSize + Mathf.Max(0f, wallVisualOverlap);
    }

    float GetWallCollisionLength()
    {
        return tileSize + Mathf.Max(0f, wallCollisionOverlap);
    }

    void AddWallBoxCollider(GameObject wallInstance, Vector3 wallCenter)
    {
        BoxCollider box = wallInstance.AddComponent<BoxCollider>();
        box.isTrigger = false;
        box.center = wallInstance.transform.InverseTransformPoint(wallCenter);

        Vector3 scale = wallInstance.transform.lossyScale;
        box.size = new Vector3(
            GetWallCollisionLength() / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
            wallHeight / Mathf.Max(0.001f, Mathf.Abs(scale.y)),
            Mathf.Max(0.01f, wallThickness) / Mathf.Max(0.001f, Mathf.Abs(scale.z))
        );
    }

    void AlignRendererBoundsCenter(GameObject target, Vector3 desiredCenter)
    {
        Bounds bounds;
        if (TryGetRendererBounds(target, out bounds))
        {
            target.transform.position += desiredCenter - bounds.center;
        }
        else
        {
            target.transform.position = desiredCenter;
        }
    }

    bool TryGetRendererBounds(GameObject target, out Bounds bounds)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            bounds = new Bounds();
            return false;
        }

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return true;
    }

    void SetStaticRecursively(GameObject target)
    {
        target.isStatic = true;
        Transform[] children = target.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            children[i].gameObject.isStatic = true;
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
                Vector3 spawnPosition;
                if (TryPickEnemySpawnPosition(room, out spawnPosition))
                {
                    enemySpawnPoints.Add(spawnPosition);
                    enemySpawnRoomDistances.Add(room.DistanceFromStart);
                }
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
        if (room.GraphStepsFromStart <= enemySafeRoomSteps) return 0;

        int dist = room.DistanceFromStart;

        if (room.Type == RoomType.Exit) return Random.Range(4, 7);
        if (dist < 15) return Random.Range(0, 2);
        if (dist < 30) return Random.Range(2, 4);
        return Random.Range(3, 6);
    }

    bool TryPickEnemySpawnPosition(RoomData room, out Vector3 spawnPosition)
    {
        for (int attempt = 0; attempt < 16; attempt++)
        {
            float sx = Random.Range(room.X + 1, room.X + room.Width - 1) * tileSize;
            float sy = Random.Range(room.Y + 1, room.Y + room.Height - 1) * tileSize;
            Vector3 candidate = new Vector3(sx, 0.5f, sy);
            if ((candidate - playerSpawnPosition).sqrMagnitude <
                minEnemySpawnDistanceFromPlayer * minEnemySpawnDistanceFromPlayer)
            {
                continue;
            }

            spawnPosition = candidate;
            return true;
        }

        spawnPosition = Vector3.zero;
        return false;
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
            GameObject torch = Instantiate(torchPrefab, pos, GetWallMountedRotation(wallNormal), dungeonParent);
            RemoveAllColliders(torch);
            EnsureTorchFlicker(torch);
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
                    GameObject torch = Instantiate(torchPrefab, pos, GetWallMountedRotation(wallDir), dungeonParent);
                    RemoveAllColliders(torch);
                    EnsureTorchFlicker(torch);
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
            Vector3 chestPosition = PickLogicalChestPosition(r);
            CreateLootChest(chestPosition);
        }
    }

    Vector3 PickLogicalChestPosition(RoomData room)
    {
        Vector2Int[] anchors =
        {
            new Vector2Int(room.X + 1, room.Y + 1),
            new Vector2Int(room.X + room.Width - 2, room.Y + 1),
            new Vector2Int(room.X + 1, room.Y + room.Height - 2),
            new Vector2Int(room.X + room.Width - 2, room.Y + room.Height - 2),
            new Vector2Int(room.Center.x, room.Y + 1),
            new Vector2Int(room.Center.x, room.Y + room.Height - 2)
        };

        int start = Random.Range(0, anchors.Length);
        for (int i = 0; i < anchors.Length; i++)
        {
            Vector2Int cell = anchors[(start + i) % anchors.Length];
            Vector3 pos = new Vector3(cell.x * tileSize, 0f, cell.y * tileSize);
            if ((pos - playerSpawnPosition).sqrMagnitude > 6f * 6f)
            {
                return pos;
            }
        }

        return new Vector3(room.Center.x * tileSize, 0f, room.Center.y * tileSize);
    }

    void CreateLootChest(Vector3 floorXZ)
    {
        float y = 0.05f + 0.22f;
        Vector3 pos = new Vector3(floorXZ.x, y, floorXZ.z);

        GameObject chest;
        if (lootChestPrefab != null)
        {
            chest = Instantiate(lootChestPrefab, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), dungeonParent);
            chest.transform.localScale = chest.transform.localScale * 0.92f;
            AlignVisualToFloor(chest, floorXZ, 0.05f);
        }
        else
        {
            chest = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chest.transform.SetParent(dungeonParent);
            chest.transform.position = pos;
            chest.transform.localScale = new Vector3(0.5f, 0.44f, 0.38f);
            ApplyFallbackChestMaterial(chest);
        }

        chest.name = "LootChest";
        chest.layer = LayerMask.NameToLayer("Default");
        IgnoreFromNavMeshBuild(chest);

        RemoveAllColliders(chest);
        BoxCollider trigger = chest.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(1.25f, 1.05f, 1.25f);
        trigger.center = new Vector3(0f, 0.48f, 0f);

        NavMeshObstacle obstacle = chest.AddComponent<NavMeshObstacle>();
        obstacle.size = new Vector3(0.82f, 0.75f, 0.82f);
        obstacle.carving = true;
        obstacle.carveOnlyStationary = true;

        LootChest loot = chest.AddComponent<LootChest>();
        loot.openSound = chestOpenClip;
    }

    void IgnoreFromNavMeshBuild(GameObject target)
    {
        Transform[] children = target.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != target.transform && children[i].GetComponent<Renderer>() == null)
            {
                continue;
            }

            NavMeshModifier modifier = children[i].GetComponent<NavMeshModifier>();
            if (modifier == null)
            {
                modifier = children[i].gameObject.AddComponent<NavMeshModifier>();
            }

            modifier.ignoreFromBuild = true;
        }
    }

    void ApplyFallbackChestMaterial(GameObject chest)
    {
        Renderer rend = chest.GetComponent<Renderer>();
        if (rend == null) return;

        if (runtimeChestMaterial == null)
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) lit = Shader.Find("Standard");
            runtimeChestMaterial = new Material(lit);
            runtimeChestMaterial.color = new Color(0.78f, 0.52f, 0.16f);
        }

        rend.sharedMaterial = runtimeChestMaterial;
    }

    void AlignVisualToFloor(GameObject target, Vector3 floorXZ, float floorY)
    {
        Bounds bounds;
        if (!TryGetRendererBounds(target, out bounds)) return;

        Vector3 correction = new Vector3(
            floorXZ.x - bounds.center.x,
            floorY - bounds.min.y,
            floorXZ.z - bounds.center.z
        );
        target.transform.position += correction;
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
            DestroyDungeonObject(colliders[i]);
        }
    }

    void DestroyDungeonObject(Object target)
    {
        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }

    void EnsureTorchFlicker(GameObject torch)
    {
        if (torch == null) return;

        Light[] lights = torch.GetComponentsInChildren<Light>(true);
        if (lights.Length == 0) return;

        for (int i = 0; i < lights.Length; i++)
        {
            lights[i].shadows = LightShadows.None;
        }

        DungeonTorch flicker = torch.GetComponent<DungeonTorch>();
        if (flicker == null)
        {
            flicker = torch.AddComponent<DungeonTorch>();
        }

        flicker.torchLight = lights[0];
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
