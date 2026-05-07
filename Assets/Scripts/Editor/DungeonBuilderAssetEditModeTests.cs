using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class DungeonBuilderAssetEditModeTests
{
    GameObject builderObject;
    GameObject parentObject;
    GameObject prefabObject;

    [TearDown]
    public void TearDown()
    {
        DestroyIfNeeded(builderObject);
        DestroyIfNeeded(parentObject);
        DestroyIfNeeded(prefabObject);
    }

    [Test]
    public void CreateWallUsesConfiguredPrefabWhenAvailable()
    {
        DungeonBuilder builder = CreateBuilder();
        prefabObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        prefabObject.name = "WallAssetPrefab";
        parentObject = new GameObject("Walls");

        FieldInfo wallPrefabsField = typeof(DungeonBuilder).GetField(
            "wallPrefabs",
            BindingFlags.Public | BindingFlags.Instance);
        Assert.That(wallPrefabsField, Is.Not.Null, "DungeonBuilder should expose wallPrefabs for imported wall assets.");
        wallPrefabsField.SetValue(builder, new[] { prefabObject });

        Invoke(builder, "CreateWall", parentObject.transform, 0, 0, Vector3.forward, null);

        Assert.That(parentObject.transform.childCount, Is.EqualTo(1));
        Transform wall = parentObject.transform.GetChild(0);
        Assert.That(wall.name, Does.StartWith("WallAssetPrefab"));
    }

    [Test]
    public void AdjacentPrefabWallsOverlapVisualBounds()
    {
        DungeonBuilder builder = CreateBuilder();
        builder.tileSize = 1f;
        builder.wallHeight = 2f;
        builder.wallThickness = 0.1f;
        prefabObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        prefabObject.name = "WallAssetPrefab";
        parentObject = new GameObject("Walls");
        SetPublicField(builder, "wallPrefabs", new[] { prefabObject });

        Invoke(builder, "CreateWall", parentObject.transform, 0, 0, Vector3.forward, null);
        Invoke(builder, "CreateWall", parentObject.transform, 1, 0, Vector3.forward, null);

        Bounds first = GetRendererBounds(parentObject.transform.GetChild(0).gameObject);
        Bounds second = GetRendererBounds(parentObject.transform.GetChild(1).gameObject);
        Assert.That(first.max.x, Is.GreaterThan(second.min.x + 0.02f));
    }

    [Test]
    public void PrefabWallGetsContinuousBoxCollider()
    {
        DungeonBuilder builder = CreateBuilder();
        builder.tileSize = 1f;
        builder.wallHeight = 2f;
        builder.wallThickness = 0.1f;
        prefabObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        DestroyIfNeeded(prefabObject.GetComponent<Collider>());
        prefabObject.name = "WallAssetPrefab";
        parentObject = new GameObject("Walls");
        SetPublicField(builder, "wallPrefabs", new[] { prefabObject });

        Invoke(builder, "CreateWall", parentObject.transform, 0, 0, Vector3.forward, null);

        BoxCollider collider = parentObject.transform.GetChild(0).GetComponent<BoxCollider>();
        Assert.That(collider, Is.Not.Null);
        Assert.That(collider.isTrigger, Is.False);
        Assert.That(collider.bounds.size.x, Is.GreaterThan(1.02f));
        Assert.That(collider.bounds.size.y, Is.EqualTo(2f).Within(0.01f));
        Assert.That(collider.bounds.size.z, Is.GreaterThanOrEqualTo(0.1f));
    }

    [Test]
    public void SpawnWallTorchFacesTowardRoomInterior()
    {
        DungeonBuilder builder = CreateBuilder();
        builder.tileSize = 1f;
        builder.wallHeight = 2.5f;

        parentObject = new GameObject("Dungeon");
        SetPrivateField(builder, "dungeonParent", parentObject.transform);

        prefabObject = new GameObject("TorchWallAsset");
        prefabObject.AddComponent<Light>();
        builder.torchPrefab = prefabObject;

        Invoke(builder, "SpawnWallTorch", 0, 0, Vector3.forward);

        Assert.That(parentObject.transform.childCount, Is.EqualTo(1));
        Transform torch = parentObject.transform.GetChild(0);
        Assert.That(Vector3.Dot(torch.forward, Vector3.back), Is.GreaterThan(0.99f));
    }

    [Test]
    public void SpawnWallTorchDisablesPrefabLightShadows()
    {
        DungeonBuilder builder = CreateBuilder();
        builder.tileSize = 1f;
        builder.wallHeight = 2.5f;

        parentObject = new GameObject("Dungeon");
        SetPrivateField(builder, "dungeonParent", parentObject.transform);

        prefabObject = new GameObject("TorchWallAsset");
        Light sourceLight = prefabObject.AddComponent<Light>();
        sourceLight.shadows = LightShadows.Soft;
        builder.torchPrefab = prefabObject;

        Invoke(builder, "SpawnWallTorch", 0, 0, Vector3.forward);

        Light spawnedLight = parentObject.transform.GetChild(0).GetComponentInChildren<Light>();
        Assert.That(spawnedLight.shadows, Is.EqualTo(LightShadows.None));
    }

    [Test]
    public void SpawnWallTorchRemovesPrefabCollidersImmediatelyInEditMode()
    {
        DungeonBuilder builder = CreateBuilder();
        builder.tileSize = 1f;
        builder.wallHeight = 2.5f;

        parentObject = new GameObject("Dungeon");
        SetPrivateField(builder, "dungeonParent", parentObject.transform);

        prefabObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        prefabObject.name = "TorchWallAsset";
        prefabObject.AddComponent<Light>();
        builder.torchPrefab = prefabObject;

        Invoke(builder, "SpawnWallTorch", 0, 0, Vector3.forward);

        Collider[] colliders = parentObject.transform.GetChild(0).GetComponentsInChildren<Collider>(true);
        Assert.That(colliders.Length, Is.EqualTo(0));
    }

    [Test]
    public void ImportedDungeonWallAndTorchAssetsAreRuntimeReady()
    {
        string[] materialPaths =
        {
            "Assets/ExternalAssets/BrokenVectorDungeon/Materials/Stone_Wall.mat",
            "Assets/ExternalAssets/BrokenVectorDungeon/Materials/Metal_Rusty.mat",
            "Assets/ExternalAssets/BrokenVectorDungeon/Materials/Wood_Basic.mat",
            "Assets/ExternalAssets/BrokenVectorDungeon/Materials/Ember.mat"
        };

        for (int i = 0; i < materialPaths.Length; i++)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPaths[i]);
            Assert.That(material, Is.Not.Null, "Material should exist: " + materialPaths[i]);
            Assert.That(material.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"), materialPaths[i]);
        }

        string[] modelPaths =
        {
            "Assets/ExternalAssets/BrokenVectorDungeon/Models/Tiles/Dungeon_Wall_Var1.fbx",
            "Assets/ExternalAssets/BrokenVectorDungeon/Models/Tiles/Dungeon_Wall_Var2.fbx",
            "Assets/ExternalAssets/BrokenVectorDungeon/Models/Tiles/Dungeon_Wall_Var3.fbx",
            "Assets/ExternalAssets/BrokenVectorDungeon/Models/Lamps/Torch_Wall.fbx"
        };

        for (int i = 0; i < modelPaths.Length; i++)
        {
            ModelImporter importer = AssetImporter.GetAtPath(modelPaths[i]) as ModelImporter;
            Assert.That(importer, Is.Not.Null, "Model importer should exist: " + modelPaths[i]);
            Assert.That(importer.isReadable, Is.True, modelPaths[i] + " should allow runtime NavMesh collection.");
            Assert.That(importer.materialLocation, Is.Not.EqualTo(ModelImporterMaterialLocation.External), modelPaths[i] + " should not use obsolete external material location.");
        }
    }

    DungeonBuilder CreateBuilder()
    {
        builderObject = new GameObject("DungeonBuilderTest");
        return builderObject.AddComponent<DungeonBuilder>();
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

    void SetPublicField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Public | BindingFlags.Instance);
        Assert.That(field, Is.Not.Null, "Field should exist: " + fieldName);
        field.SetValue(target, value);
    }

    Bounds GetRendererBounds(GameObject target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        Assert.That(renderers.Length, Is.GreaterThan(0), "Target should have renderers: " + target.name);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    void DestroyIfNeeded(GameObject target)
    {
        if (target != null)
        {
            Object.DestroyImmediate(target);
        }
    }

    void DestroyIfNeeded(Object target)
    {
        if (target != null)
        {
            Object.DestroyImmediate(target);
        }
    }
}
