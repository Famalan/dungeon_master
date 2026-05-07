using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.UI;

public class DungeonGenerationPolishEditModeTests
{
    readonly List<GameObject> createdObjects = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < createdObjects.Count; i++)
        {
            if (createdObjects[i] != null)
            {
                Object.DestroyImmediate(createdObjects[i]);
            }
        }

        createdObjects.Clear();
    }

    [Test]
    public void CorridorGraphConnectsNearestRoomsInsteadOfPlacementOrder()
    {
        GameObject go = CreateObject("Generator");
        DungeonGenerator generator = go.AddComponent<DungeonGenerator>();

        List<RoomData> rooms = new List<RoomData>
        {
            CreateRoom(0, 0, RoomType.Start),
            CreateRoom(50, 0, RoomType.Exit),
            CreateRoom(6, 0, RoomType.Normal),
            CreateRoom(12, 0, RoomType.Normal)
        };

        SetPrivateSetProperty(generator, "Rooms", rooms);
        SetPrivateSetProperty(generator, "Corridors", new List<CorridorData>());

        Invoke(generator, "CreateCorridors");

        Assert.That(rooms[0].ConnectedRoomIndexes, Has.Member(2),
            "Start room should first connect into the nearest local cluster, not to the next randomly placed room.");
        Assert.That(rooms[0].ConnectedRoomIndexes, Has.No.Member(1),
            "Sequential placement-order connection creates long, ugly corridors across the map.");
    }

    [Test]
    public void LootChestIgnoresEveryChildMeshDuringNavMeshBuild()
    {
        GameObject builderObject = CreateObject("DungeonBuilderTest");
        DungeonBuilder builder = builderObject.AddComponent<DungeonBuilder>();

        GameObject dungeonObject = CreateObject("Dungeon");
        SetPrivateField(builder, "dungeonParent", dungeonObject.transform);

        GameObject chestPrefab = CreateObject("ReadableChestVisual");
        GameObject lid = GameObject.CreatePrimitive(PrimitiveType.Cube);
        createdObjects.Add(lid);
        lid.name = "Chest_Lid";
        lid.transform.SetParent(chestPrefab.transform);
        builder.lootChestPrefab = chestPrefab;

        Invoke(builder, "CreateLootChest", Vector3.zero);

        Transform chest = dungeonObject.transform.Find("LootChest");
        Assert.That(chest, Is.Not.Null);

        Transform[] children = chest.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].GetComponent<Renderer>() == null) continue;

            NavMeshModifier modifier = children[i].GetComponent<NavMeshModifier>();
            Assert.That(modifier, Is.Not.Null, children[i].name);
            Assert.That(modifier.ignoreFromBuild, Is.True, children[i].name);
        }
    }

    [Test]
    public void StartRoomDoesNotSpawnFloorPropPrefabsOnPlayerSpawn()
    {
        GameObject generatorObject = CreateObject("Generator");
        DungeonGenerator generator = generatorObject.AddComponent<DungeonGenerator>();
        SetPrivateSetProperty(generator, "Rooms", new List<RoomData>
        {
            new RoomData { X = 2, Y = 2, Width = 8, Height = 8, Type = RoomType.Start }
        });

        GameObject decorObject = CreateObject("Decor");
        DungeonDecorLayer decor = decorObject.AddComponent<DungeonDecorLayer>();
        decor.prefabPropChance = 1f;
        decor.floorPropPrefabs = new[] { CreateObject("FloorPropPrefab") };
        decor.wallPropPrefabs = new GameObject[0];

        GameObject dungeonObject = CreateObject("Dungeon");
        decor.Decorate(dungeonObject.transform, generator, 1f, 2.5f);

        Assert.That(FindChildByPrefix(dungeonObject.transform, "FloorPropPrefab"), Is.Null,
            "The start room should stay readable and clear around the player spawn.");
    }

    [Test]
    public void GameUIAddsCompassSpriteWithoutSharingTextGraphicObject()
    {
        GameObject canvas = CreateObject("Canvas");
        GameUI ui = canvas.AddComponent<GameUI>();
        ui.compassArrowSprite = Sprite.Create(new Texture2D(4, 4), new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));

        GameObject hud = CreateObject("HUDPanel");
        hud.transform.SetParent(canvas.transform);
        ui.hudPanel = hud;

        GameObject compass = CreateObject("ExitCompassRow");
        compass.transform.SetParent(hud.transform);
        compass.AddComponent<RectTransform>();

        GameObject arrow = CreateObject("ExitCompassArrow");
        arrow.transform.SetParent(compass.transform);
        arrow.AddComponent<RectTransform>();
        arrow.AddComponent<Text>();

        Assert.DoesNotThrow(() => Invoke(ui, "EnsureRuntimeHudElements"));
        Assert.That(arrow.GetComponent<Text>().enabled, Is.False);
        Assert.That(FindChildByPrefix(arrow.transform, "ExitCompassArrowImage"), Is.Not.Null);
    }

    GameObject CreateObject(string name)
    {
        GameObject go = new GameObject(name);
        createdObjects.Add(go);
        return go;
    }

    RoomData CreateRoom(int x, int y, RoomType type)
    {
        return new RoomData
        {
            X = x,
            Y = y,
            Width = 4,
            Height = 4,
            Type = type
        };
    }

    Transform FindChildByPrefix(Transform root, string prefix)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name.StartsWith(prefix))
            {
                return children[i];
            }
        }

        return null;
    }

    void Invoke(object target, string methodName, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(method, Is.Not.Null, "Method should exist: " + methodName);
        method.Invoke(target, args);
    }

    void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(field, Is.Not.Null, "Field should exist: " + fieldName);
        field.SetValue(target, value);
    }

    void SetPrivateSetProperty(object target, string propertyName, object value)
    {
        PropertyInfo property = target.GetType().GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.Instance);
        Assert.That(property, Is.Not.Null, "Property should exist: " + propertyName);
        property.SetValue(target, value);
    }
}
