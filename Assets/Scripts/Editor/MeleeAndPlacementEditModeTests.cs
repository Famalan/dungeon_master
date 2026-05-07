using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class MeleeAndPlacementEditModeTests
{
    readonly List<GameObject> createdObjects = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < createdObjects.Count; i++)
        {
            if (createdObjects[i] != null)
            {
                UnityEngine.Object.DestroyImmediate(createdObjects[i]);
            }
        }

        createdObjects.Clear();
    }

    [Test]
    public void FloorDecorPrefabIsAlignedToFloorEvenWhenModelPivotIsOffset()
    {
        GameObject generatorObject = CreateObject("Generator");
        DungeonGenerator generator = generatorObject.AddComponent<DungeonGenerator>();
        SetProperty(generator, "Rooms", new List<RoomData>
        {
            new RoomData { X = 2, Y = 2, Width = 8, Height = 8, Type = RoomType.Normal }
        });

        GameObject prefab = CreateObject("FloatingFloorPrefab");
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "OffsetVisual";
        visual.transform.SetParent(prefab.transform, false);
        visual.transform.localPosition = new Vector3(0f, 2f, 0f);

        GameObject decorObject = CreateObject("Decor");
        DungeonDecorLayer decor = decorObject.AddComponent<DungeonDecorLayer>();
        decor.prefabPropChance = 1f;
        decor.minNormalRoomProps = 1;
        decor.maxNormalRoomProps = 1;
        decor.floorPropPrefabs = new[] { prefab };
        decor.wallPropPrefabs = Array.Empty<GameObject>();
        decor.lightPrefabs = Array.Empty<GameObject>();

        GameObject dungeonObject = CreateObject("Dungeon");
        decor.Decorate(dungeonObject.transform, generator, 1f, 2.5f);

        Transform instance = FindChildByPrefix(dungeonObject.transform, "FloatingFloorPrefab");
        Assert.That(instance, Is.Not.Null);

        Bounds bounds = GetRendererBounds(instance.gameObject);
        Assert.That(bounds.min.y, Is.InRange(0.045f, 0.085f),
            "Floor props with offset pivots should be corrected by renderer bounds, not left hanging.");
    }

    [Test]
    public void GameSceneSetupDoesNotAssignDecorativeSmallChestsAsRandomFloorProps()
    {
        GameSceneSetup setup = ScriptableObject.CreateInstance<GameSceneSetup>();
        try
        {
            GameObject decorObject = CreateObject("Decor");
            DungeonDecorLayer decor = decorObject.AddComponent<DungeonDecorLayer>();

            Invoke(setup, "AssignDecorPrefabs", decor);

            Assert.That(decor.floorPropPrefabs, Is.Not.Null);
            for (int i = 0; i < decor.floorPropPrefabs.Length; i++)
            {
                Assert.That(decor.floorPropPrefabs[i].name, Does.Not.Contain("ChestSmall"));
                Assert.That(decor.floorPropPrefabs[i].name, Does.Not.EqualTo("Chest"));
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(setup);
        }
    }

    [Test]
    public void DungeonComputesGraphStepsAndSkipsEnemySpawnsInFirstConnectedRoom()
    {
        Assert.That(typeof(RoomData).GetField("GraphStepsFromStart"), Is.Not.Null,
            "RoomData should track graph steps, not only world distance.");

        GameObject generatorObject = CreateObject("Generator");
        DungeonGenerator generator = generatorObject.AddComponent<DungeonGenerator>();
        List<RoomData> rooms = new List<RoomData>
        {
            CreateRoom(0, 0, RoomType.Start),
            CreateRoom(7, 0, RoomType.Normal),
            CreateRoom(18, 0, RoomType.Normal)
        };
        rooms[0].ConnectedRoomIndexes.Add(1);
        rooms[1].ConnectedRoomIndexes.Add(0);
        rooms[1].ConnectedRoomIndexes.Add(2);
        rooms[2].ConnectedRoomIndexes.Add(1);
        SetProperty(generator, "Rooms", rooms);

        Invoke(generator, "ComputeDistances");

        Assert.That(rooms[1].GraphStepsFromStart, Is.EqualTo(1));
        Assert.That(rooms[2].GraphStepsFromStart, Is.EqualTo(2));

        GameObject builderObject = CreateObject("Builder");
        DungeonBuilder builder = builderObject.AddComponent<DungeonBuilder>();
        builder.generator = generator;
        builder.tileSize = 1f;
        SetField(builder, "dungeonParent", CreateObject("Dungeon").transform);

        Invoke(builder, "PlaceSpecialObjects");

        Assert.That(builder.EnemySpawnPoints.Count, Is.GreaterThan(0),
            "Rooms beyond the one-room start buffer should still create regular enemy spawn points.");
        for (int i = 0; i < builder.EnemySpawnPoints.Count; i++)
        {
            Assert.That(builder.EnemySpawnPoints[i].x, Is.GreaterThanOrEqualTo(18f),
                "No enemy should spawn in the first connected room next to the start.");
        }
    }

    [Test]
    public void MeleeWeaponControllerDamagesEnemyInReach()
    {
        Type modeType = GetProjectType("MeleeWeaponMode");
        Type controllerType = GetProjectType("MeleeWeaponController");
        Assert.That(modeType, Is.Not.Null);
        Assert.That(controllerType, Is.Not.Null);

        GameObject player = CreateObject("Player");
        Component controller = player.AddComponent(controllerType);
        SetField(controller, "attackOrigin", player.transform);
        SetField(controller, "activeMode", Enum.Parse(modeType, "Sword"));
        SetField(controller, "damageMultiplierSource", null);

        GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        createdObjects.Add(enemy);
        enemy.name = "Enemy";
        enemy.tag = "Enemy";
        enemy.transform.position = new Vector3(0f, 0f, 1.25f);
        HealthSystem health = enemy.AddComponent<HealthSystem>();
        health.maxHealth = 50;
        health.currentHealth = 50;
        health.onHealthChanged = new UnityEngine.Events.UnityEvent<int, int>();
        health.onDamageTaken = new UnityEngine.Events.UnityEvent();
        health.onDeath = new UnityEngine.Events.UnityEvent();

        Physics.SyncTransforms();
        object didAttack = Invoke(controller, "TryAttack");

        Assert.That(didAttack, Is.EqualTo(true));
        Assert.That(health.currentHealth, Is.LessThan(50));
    }

    [Test]
    public void SpearDamagesEnemyAtLongerReach()
    {
        Type modeType = GetProjectType("MeleeWeaponMode");
        Type controllerType = GetProjectType("MeleeWeaponController");
        Assert.That(modeType, Is.Not.Null);
        Assert.That(controllerType, Is.Not.Null);

        GameObject player = CreateObject("Player");
        Component controller = player.AddComponent(controllerType);
        SetField(controller, "attackOrigin", player.transform);
        SetField(controller, "activeMode", Enum.Parse(modeType, "Spear"));

        GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        createdObjects.Add(enemy);
        enemy.name = "Enemy";
        enemy.tag = "Enemy";
        enemy.transform.position = new Vector3(0f, 0f, 3.25f);
        HealthSystem health = enemy.AddComponent<HealthSystem>();
        health.maxHealth = 50;
        health.currentHealth = 50;
        health.onHealthChanged = new UnityEngine.Events.UnityEvent<int, int>();
        health.onDamageTaken = new UnityEngine.Events.UnityEvent();
        health.onDeath = new UnityEngine.Events.UnityEvent();

        Physics.SyncTransforms();
        object didAttack = Invoke(controller, "TryAttack");

        Assert.That(didAttack, Is.EqualTo(true));
        Assert.That(health.currentHealth, Is.LessThan(50),
            "Spear should act as the longer-range melee option.");
    }

    [Test]
    public void WeaponViewModelControllerShowsOnlyTheSelectedMeleeWeapon()
    {
        Type modeType = GetProjectType("MeleeWeaponMode");
        Type controllerType = GetProjectType("WeaponViewModelController");
        Assert.That(modeType, Is.Not.Null);
        Assert.That(controllerType, Is.Not.Null);

        GameObject root = CreateObject("ViewModelRoot");
        Component controller = root.AddComponent(controllerType);

        GameObject sword = CreateChild(root.transform, "SwordModel");
        GameObject axe = CreateChild(root.transform, "AxeModel");
        GameObject spear = CreateChild(root.transform, "SpearModel");
        GameObject hammer = CreateChild(root.transform, "HammerModel");

        SetField(controller, "swordModel", sword);
        SetField(controller, "axeModel", axe);
        SetField(controller, "spearModel", spear);
        SetField(controller, "hammerModel", hammer);

        Invoke(controller, "SetMode", Enum.Parse(modeType, "Hammer"));

        Assert.That(sword.activeSelf, Is.False);
        Assert.That(axe.activeSelf, Is.False);
        Assert.That(spear.activeSelf, Is.False);
        Assert.That(hammer.activeSelf, Is.True);
    }

    [Test]
    public void WeaponViewModelDefaultsKeepWeaponLargeAndSwingReadable()
    {
        GameObject root = CreateObject("ViewModelRoot");
        WeaponViewModelController controller = root.AddComponent<WeaponViewModelController>();

        Assert.That(controller.restLocalPosition.x, Is.GreaterThanOrEqualTo(0.5f),
            "Weapon should sit visibly in the lower-right frame.");
        Assert.That(controller.restLocalPosition.z, Is.GreaterThanOrEqualTo(0.78f),
            "Weapon should be pushed forward enough to read as a held weapon.");
        Assert.That(controller.swingLocalEulerOffset.magnitude, Is.GreaterThanOrEqualTo(72f),
            "Swing animation should be visibly readable in first person.");
    }

    [Test]
    public void ExitCompassAppliesSpriteOrientationOffset()
    {
        FieldInfo offsetField = typeof(ExitCompassUI).GetField("arrowSpriteAngleOffsetDegrees");
        Assert.That(offsetField, Is.Not.Null);

        GameObject compassObject = CreateObject("Compass");
        ExitCompassUI compass = compassObject.AddComponent<ExitCompassUI>();
        offsetField.SetValue(compass, 180f);

        GameObject arrow = CreateObject("Arrow");
        RectTransform arrowRoot = arrow.AddComponent<RectTransform>();
        compass.arrowRoot = arrowRoot;

        GameObject player = CreateObject("Player");
        player.AddComponent<CharacterController>();
        player.AddComponent<PlayerController>();
        player.transform.position = Vector3.zero;
        player.transform.rotation = Quaternion.identity;

        GameObject builderObject = CreateObject("Builder");
        DungeonBuilder builder = builderObject.AddComponent<DungeonBuilder>();
        SetField(builder, "exitPosition", new Vector3(0f, 0f, 10f));
        SetField(compass, "playerTransform", player.transform);
        SetField(compass, "dungeonBuilder", builder);

        Invoke(compass, "LateUpdate");

        Assert.That(Mathf.DeltaAngle(180f, arrowRoot.localEulerAngles.z), Is.EqualTo(0f).Within(0.5f));
    }

    [Test]
    public void ArtifactButtonsUseBrighterTagColors()
    {
        GameObject canvas = CreateObject("Canvas");
        GameUI ui = canvas.AddComponent<GameUI>();

        Button button = CreateObject("ArtifactButton").AddComponent<Button>();
        button.gameObject.AddComponent<Image>();
        Text text = CreateObject("ArtifactText").AddComponent<Text>();
        button.transform.SetParent(canvas.transform);
        text.transform.SetParent(button.transform);

        ui.perkButtons = new[] { button, null, null };
        ui.perkButtonTexts = new[] { text, null, null };
        SetField(ui, "currentArtifacts", new[]
        {
            new ArtifactData("test", "Ruby Fang", "+damage", ArtifactRarity.Rare, ArtifactTag.Flame, Color.red, s => { })
        });

        Invoke(ui, "SetupArtifactButtons");

        Image image = button.GetComponent<Image>();
        Assert.That(image.color.r, Is.GreaterThan(0.55f),
            "Artifact selection cards should read brighter than the old muted dark blend.");
    }

    GameObject CreateObject(string name)
    {
        GameObject go = new GameObject(name);
        createdObjects.Add(go);
        return go;
    }

    GameObject CreateChild(Transform parent, string name)
    {
        GameObject child = CreateObject(name);
        child.transform.SetParent(parent, false);
        return child;
    }

    RoomData CreateRoom(int x, int y, RoomType type)
    {
        return new RoomData
        {
            X = x,
            Y = y,
            Width = 6,
            Height = 6,
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

    Bounds GetRendererBounds(GameObject target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        Assert.That(renderers.Length, Is.GreaterThan(0));

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    Type GetProjectType(string typeName)
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < assemblies.Length; i++)
        {
            Type type = assemblies[i].GetType(typeName);
            if (type != null)
            {
                return type;
            }
        }

        return null;
    }

    object Invoke(object target, string methodName, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(method, Is.Not.Null, "Method should exist: " + methodName);
        return method.Invoke(target, args);
    }

    void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(field, Is.Not.Null, "Field should exist: " + fieldName);
        field.SetValue(target, value);
    }

    void SetProperty(object target, string propertyName, object value)
    {
        PropertyInfo property = target.GetType().GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(property, Is.Not.Null, "Property should exist: " + propertyName);
        property.SetValue(target, value);
    }
}
