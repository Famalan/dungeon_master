using System.Collections.Generic;
using UnityEngine;

public class DungeonDecorLayer : MonoBehaviour
{
    [Header("Optional Prefab Props")]
    public GameObject[] floorPropPrefabs;
    public GameObject[] wallPropPrefabs;
    public GameObject[] lightPrefabs;

    [Header("Density")]
    [Range(0f, 1f)] public float prefabPropChance = 0f;
    public int minNormalRoomProps = 2;
    public int maxNormalRoomProps = 5;
    public bool usePrimitiveFallback = false;

    Material columnMaterial;
    Material bannerMaterial;
    Material rockMaterial;
    Material trapMaterial;

    public void Decorate(Transform dungeonParent, DungeonGenerator generator, float tileSize, float wallHeight)
    {
        Decorate(dungeonParent, generator, tileSize, wallHeight, new DungeonPlacementContext());
    }

    public void Decorate(Transform dungeonParent, DungeonGenerator generator, float tileSize, float wallHeight,
        DungeonPlacementContext placementContext)
    {
        if (dungeonParent == null || generator == null || generator.Rooms == null) return;
        if (placementContext == null) placementContext = new DungeonPlacementContext();

        EnsureMaterials();

        Transform propsParent = new GameObject("Props").transform;
        propsParent.SetParent(dungeonParent, false);

        List<RoomData> rooms = generator.Rooms;
        for (int i = 0; i < rooms.Count; i++)
        {
            RoomData room = rooms[i];
            Transform roomRoot = new GameObject("RoomProps_" + room.Type + "_" + i).transform;
            roomRoot.SetParent(propsParent, false);

            Color accent = GetRoomAccent(room, i);
            CreateRoomAccentLight(roomRoot, room, tileSize, wallHeight, accent);

            if (room.Width >= 7 && room.Height >= 7)
            {
                CreateCornerColumns(roomRoot, room, tileSize, wallHeight, placementContext);
            }

            CreateWallDressings(roomRoot, room, tileSize, wallHeight, accent, placementContext);
            CreateFloorProps(roomRoot, room, tileSize, placementContext);

            if (room.Type == RoomType.Start)
            {
                CreateStartDressing(roomRoot, room, tileSize, accent);
            }
            else if (room.Type == RoomType.Exit)
            {
                CreateExitDressing(roomRoot, room, tileSize, wallHeight, accent);
            }
        }
    }

    void CreateFloorProps(Transform parent, RoomData room, float tileSize, DungeonPlacementContext placementContext)
    {
        if (room.Width < 5 || room.Height < 5) return;
        if (room.Type == RoomType.Start) return;

        int area = room.Width * room.Height;
        int count = Mathf.Clamp(area / 28, minNormalRoomProps, maxNormalRoomProps);
        if (room.Type == RoomType.Exit) count += 2;

        for (int i = 0; i < count; i++)
        {
            if (TrySpawnFloorPrefab(parent, room, tileSize, placementContext, i))
            {
                continue;
            }

            if (!usePrimitiveFallback)
            {
                continue;
            }

            Vector3 pos = RandomRoomPosition(room, tileSize, 1.7f, 0.05f);
            float primitiveRadius = 0.65f;
            if (!placementContext.CanPlaceCircle(pos, primitiveRadius))
            {
                continue;
            }

            CreatePrimitiveProp(parent, pos, i);
            placementContext.ReserveCircle(pos, primitiveRadius);
        }
    }

    void CreateWallDressings(Transform parent, RoomData room, float tileSize, float wallHeight, Color accent,
        DungeonPlacementContext placementContext)
    {
        if (room.Width < 6 || room.Height < 6) return;

        int count = room.Type == RoomType.Exit ? 4 : 2;
        for (int i = 0; i < count; i++)
        {
            bool northSouth = Random.value > 0.5f;
            float x = Random.Range(room.X + 1.5f, room.X + room.Width - 1.5f) * tileSize;
            float z = Random.Range(room.Y + 1.5f, room.Y + room.Height - 1.5f) * tileSize;
            Vector3 pos;
            Quaternion rot;

            if (northSouth)
            {
                bool north = Random.value > 0.5f;
                pos = new Vector3(x, wallHeight * 0.48f, (north ? room.Y + room.Height - 0.55f : room.Y + 0.55f) * tileSize);
                rot = Quaternion.Euler(0f, north ? 180f : 0f, 0f);
            }
            else
            {
                bool east = Random.value > 0.5f;
                pos = new Vector3((east ? room.X + room.Width - 0.55f : room.X + 0.55f) * tileSize, wallHeight * 0.48f, z);
                rot = Quaternion.Euler(0f, east ? -90f : 90f, 0f);
            }

            if (TrySpawnWallPrefab(parent, wallPropPrefabs, pos, rot, 0.7f, placementContext))
            {
                continue;
            }

            if (!usePrimitiveFallback)
            {
                continue;
            }

            if (!placementContext.CanPlaceCircle(pos, tileSize * 0.72f))
            {
                continue;
            }

            GameObject banner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            banner.name = "AccentBanner";
            banner.transform.SetParent(parent, false);
            banner.transform.position = pos;
            banner.transform.rotation = rot;
            banner.transform.localScale = new Vector3(0.08f, wallHeight * 0.45f, tileSize * 0.62f);
            AssignMaterial(banner, bannerMaterial, accent);
            DisableColliders(banner);
            placementContext.ReserveCircle(pos, tileSize * 0.72f);
        }
    }

    void CreateCornerColumns(Transform parent, RoomData room, float tileSize, float wallHeight,
        DungeonPlacementContext placementContext)
    {
        Vector2[] corners =
        {
            new Vector2(room.X + 1.4f, room.Y + 1.4f),
            new Vector2(room.X + room.Width - 1.4f, room.Y + 1.4f),
            new Vector2(room.X + 1.4f, room.Y + room.Height - 1.4f),
            new Vector2(room.X + room.Width - 1.4f, room.Y + room.Height - 1.4f)
        };

        for (int i = 0; i < corners.Length; i++)
        {
            GameObject column = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            column.name = "LowPolyColumn";
            column.transform.SetParent(parent, false);
            column.transform.position = new Vector3(corners[i].x * tileSize, wallHeight * 0.5f, corners[i].y * tileSize);
            column.transform.localScale = new Vector3(0.28f, wallHeight * 0.5f, 0.28f);
            AssignMaterial(column, columnMaterial, Color.white);
            DisableColliders(column);
            placementContext.ReserveCircle(column.transform.position, tileSize * 0.55f);
        }
    }

    void CreateStartDressing(Transform parent, RoomData room, float tileSize, Color accent)
    {
        Vector3 center = RoomCenter(room, tileSize, 0.06f);
        GameObject rug = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rug.name = "StartRunicRug";
        rug.transform.SetParent(parent, false);
        rug.transform.position = center;
        rug.transform.localScale = new Vector3(tileSize * 2.4f, 0.04f, tileSize * 1.4f);
        AssignMaterial(rug, bannerMaterial, accent);
        DisableColliders(rug);
    }

    void CreateExitDressing(Transform parent, RoomData room, float tileSize, float wallHeight, Color accent)
    {
        Vector3 center = RoomCenter(room, tileSize, 0.08f);
        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        platform.name = "ExitPlatform";
        platform.transform.SetParent(parent, false);
        platform.transform.position = center;
        platform.transform.localScale = new Vector3(tileSize * 1.65f, 0.08f, tileSize * 1.65f);
        AssignMaterial(platform, bannerMaterial, accent);
        DisableColliders(platform);

        CreateRoomAccentLight(parent, room, tileSize, wallHeight, accent * 1.4f);
    }

    void CreateRoomAccentLight(Transform parent, RoomData room, float tileSize, float wallHeight, Color accent)
    {
        if (TrySpawnPrefab(parent, lightPrefabs, RoomCenter(room, tileSize, wallHeight * 0.65f), Quaternion.identity, 0.75f))
        {
            return;
        }

        GameObject lightObj = new GameObject("RoomAccentLight");
        lightObj.transform.SetParent(parent, false);
        lightObj.transform.position = RoomCenter(room, tileSize, wallHeight * 0.78f);

        Light point = lightObj.AddComponent<Light>();
        point.type = LightType.Point;
        point.range = room.Type == RoomType.Exit ? 7f : 4.8f;
        point.intensity = room.Type == RoomType.Exit ? 1.55f : 0.55f;
        point.color = accent;
    }

    bool TrySpawnPrefab(Transform parent, GameObject[] prefabs, Vector3 position, Quaternion rotation, float propScale)
    {
        if (prefabs == null || prefabs.Length == 0) return false;
        if (Random.value > prefabPropChance) return false;

        GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
        if (prefab == null) return false;

        GameObject instance = Instantiate(prefab, position, rotation, parent);
        instance.name = prefab.name + "_Decor";
        instance.transform.localScale = instance.transform.localScale * propScale;
        DisableColliders(instance);
        return true;
    }

    bool TrySpawnFloorPrefab(Transform parent, RoomData room, float tileSize, DungeonPlacementContext placementContext,
        int placementIndex)
    {
        if (floorPropPrefabs == null || floorPropPrefabs.Length == 0) return false;
        if (Random.value > prefabPropChance) return false;

        const int maxAttempts = 12;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            GameObject prefab = PickAllowedFloorPrefab();
            if (prefab == null) return false;

            float propScale = IsTallFloorProp(prefab) ? 0.82f : 0.68f;
            float radius = GetPrefabFootprintRadius(prefab, propScale);
            Vector3 pos = PickFloorPropPosition(room, tileSize,
                Mathf.Max(1.35f, radius / Mathf.Max(0.01f, tileSize) + 0.55f), placementIndex, attempt, 0.05f);
            if (!placementContext.CanPlaceCircle(pos, radius + 0.18f))
            {
                continue;
            }

            Quaternion rotation = Quaternion.Euler(0f, GetLogicalPropYaw(room, pos, tileSize), 0f);
            GameObject instance = Instantiate(prefab, pos, rotation, parent);
            instance.name = prefab.name + "_Decor";
            instance.transform.localScale = instance.transform.localScale * propScale;
            AlignRendererBoundsToFloor(instance, 0.05f, pos);
            DisableColliders(instance);

            Bounds bounds;
            if (TryGetRendererBounds(instance, out bounds))
            {
                radius = Mathf.Max(radius, Mathf.Max(bounds.extents.x, bounds.extents.z) + 0.16f);
            }

            placementContext.ReserveCircle(instance.transform.position, radius);
            return true;
        }

        return false;
    }

    bool TrySpawnWallPrefab(Transform parent, GameObject[] prefabs, Vector3 position, Quaternion rotation,
        float propScale, DungeonPlacementContext placementContext)
    {
        if (prefabs == null || prefabs.Length == 0) return false;
        if (Random.value > prefabPropChance) return false;

        for (int attempt = 0; attempt < 8; attempt++)
        {
            GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
            if (prefab == null) return false;

            float radius = GetPrefabFootprintRadius(prefab, propScale) + 0.18f;
            if (!placementContext.CanPlaceCircle(position, radius))
            {
                return false;
            }

            GameObject instance = Instantiate(prefab, position, rotation, parent);
            instance.name = prefab.name + "_Decor";
            instance.transform.localScale = instance.transform.localScale * propScale;
            DisableColliders(instance);
            placementContext.ReserveCircle(position, radius);
            return true;
        }

        return false;
    }

    GameObject PickAllowedFloorPrefab()
    {
        if (floorPropPrefabs == null || floorPropPrefabs.Length == 0) return null;

        int start = Random.Range(0, floorPropPrefabs.Length);
        for (int i = 0; i < floorPropPrefabs.Length; i++)
        {
            GameObject prefab = floorPropPrefabs[(start + i) % floorPropPrefabs.Length];
            if (prefab == null) continue;
            if (IsDecorativeChest(prefab)) continue;
            return prefab;
        }

        return null;
    }

    bool IsDecorativeChest(GameObject prefab)
    {
        string n = prefab.name.ToLowerInvariant();
        return n.Contains("chestsmall") || n == "chest" || n.EndsWith("/chest");
    }

    bool IsTallFloorProp(GameObject prefab)
    {
        string n = prefab.name.ToLowerInvariant();
        return n.Contains("barrel") || n.Contains("table") || n.Contains("bench");
    }

    float GetPrefabFootprintRadius(GameObject prefab, float propScale)
    {
        Bounds bounds;
        if (!TryGetRendererBounds(prefab, out bounds))
        {
            return 0.62f;
        }

        return Mathf.Clamp(Mathf.Max(bounds.extents.x, bounds.extents.z) * propScale, 0.42f, 1.25f);
    }

    void AlignRendererBoundsToFloor(GameObject target, float floorY, Vector3 desiredXZ)
    {
        Bounds bounds;
        if (!TryGetRendererBounds(target, out bounds)) return;

        Vector3 correction = new Vector3(
            desiredXZ.x - bounds.center.x,
            floorY - bounds.min.y,
            desiredXZ.z - bounds.center.z
        );
        target.transform.position += correction;
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

    void CreatePrimitiveProp(Transform parent, Vector3 pos, int index)
    {
        int kind = Random.Range(0, 4);
        PrimitiveType primitive = kind == 0 ? PrimitiveType.Cylinder : (kind == 1 ? PrimitiveType.Cube : PrimitiveType.Sphere);
        GameObject prop = GameObject.CreatePrimitive(primitive);
        prop.name = "DungeonProp";
        prop.transform.SetParent(parent, false);
        prop.transform.position = pos;
        prop.transform.rotation = Quaternion.Euler(0f, Random.Range(0, 360f), 0f);

        if (kind == 0)
        {
            prop.transform.localScale = new Vector3(0.32f, 0.45f, 0.32f);
            AssignMaterial(prop, columnMaterial, Color.white);
        }
        else if (kind == 1)
        {
            prop.transform.localScale = new Vector3(0.55f, 0.35f, 0.45f);
            AssignMaterial(prop, columnMaterial, new Color(0.72f, 0.55f, 0.34f, 1f));
        }
        else if (kind == 2)
        {
            prop.transform.localScale = new Vector3(0.45f, 0.18f, 0.34f);
            AssignMaterial(prop, rockMaterial, Color.white);
        }
        else
        {
            prop.transform.localScale = new Vector3(0.9f, 0.05f, 0.9f);
            AssignMaterial(prop, trapMaterial, Color.white);
        }

        DisableColliders(prop);
    }

    Vector3 RandomRoomPosition(RoomData room, float tileSize, float inset, float y)
    {
        float minX = room.X + inset;
        float maxX = room.X + room.Width - inset;
        float minZ = room.Y + inset;
        float maxZ = room.Y + room.Height - inset;
        float x = maxX > minX ? Random.Range(minX, maxX) : room.Center.x;
        float z = maxZ > minZ ? Random.Range(minZ, maxZ) : room.Center.y;
        x *= tileSize;
        z *= tileSize;
        return new Vector3(x, y, z);
    }

    Vector3 PickFloorPropPosition(RoomData room, float tileSize, float inset, int placementIndex, int attempt, float y)
    {
        float left = room.X + inset;
        float right = room.X + room.Width - inset;
        float bottom = room.Y + inset;
        float top = room.Y + room.Height - inset;

        Vector2[] anchors =
        {
            new Vector2(left, bottom),
            new Vector2(right, bottom),
            new Vector2(left, top),
            new Vector2(right, top),
            new Vector2(room.Center.x, bottom),
            new Vector2(room.Center.x, top),
            new Vector2(left, room.Center.y),
            new Vector2(right, room.Center.y)
        };

        Vector2 anchor = anchors[Mathf.Abs(placementIndex + attempt) % anchors.Length];
        float jitter = Mathf.Min(0.45f, tileSize * 0.38f);
        float x = Mathf.Clamp(anchor.x * tileSize + Random.Range(-jitter, jitter), left * tileSize, right * tileSize);
        float z = Mathf.Clamp(anchor.y * tileSize + Random.Range(-jitter, jitter), bottom * tileSize, top * tileSize);
        return new Vector3(x, y, z);
    }

    float GetLogicalPropYaw(RoomData room, Vector3 position, float tileSize)
    {
        Vector3 center = RoomCenter(room, tileSize, position.y);
        Vector3 toCenter = center - position;
        toCenter.y = 0f;
        if (toCenter.sqrMagnitude < 0.01f)
        {
            return Random.Range(0f, 360f);
        }

        return Quaternion.LookRotation(toCenter.normalized, Vector3.up).eulerAngles.y;
    }

    Vector3 RoomCenter(RoomData room, float tileSize, float y)
    {
        return new Vector3(room.Center.x * tileSize, y, room.Center.y * tileSize);
    }

    Color GetRoomAccent(RoomData room, int index)
    {
        if (room.Type == RoomType.Start) return new Color(0.25f, 0.76f, 0.92f, 1f);
        if (room.Type == RoomType.Exit) return new Color(1f, 0.56f, 0.14f, 1f);

        Color[] accents =
        {
            new Color(0.86f, 0.22f, 0.18f, 1f),
            new Color(0.42f, 0.38f, 0.92f, 1f),
            new Color(0.24f, 0.72f, 0.44f, 1f),
            new Color(0.95f, 0.78f, 0.22f, 1f)
        };
        return accents[Mathf.Abs(index) % accents.Length];
    }

    void EnsureMaterials()
    {
        if (columnMaterial == null) columnMaterial = CreateMaterial("DecorStone", new Color(0.28f, 0.25f, 0.31f, 1f));
        if (bannerMaterial == null) bannerMaterial = CreateMaterial("DecorAccent", new Color(0.72f, 0.18f, 0.28f, 1f));
        if (rockMaterial == null) rockMaterial = CreateMaterial("DecorRock", new Color(0.18f, 0.17f, 0.2f, 1f));
        if (trapMaterial == null) trapMaterial = CreateMaterial("DecorTrap", new Color(0.38f, 0.08f, 0.08f, 1f));
    }

    Material CreateMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material material = new Material(shader);
        material.name = name;
        material.color = color;
        return material;
    }

    void AssignMaterial(GameObject target, Material material, Color tint)
    {
        Renderer renderer = target.GetComponentInChildren<Renderer>();
        if (renderer == null || material == null) return;

        renderer.sharedMaterial = material;
        if (tint != Color.white)
        {
            Material instance = new Material(material);
            instance.color = tint;
            renderer.sharedMaterial = instance;
        }
    }

    void DisableColliders(GameObject root)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
    }
}
