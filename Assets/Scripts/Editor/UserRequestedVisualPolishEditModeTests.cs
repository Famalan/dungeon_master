using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

public class UserRequestedVisualPolishEditModeTests
{
    GameObject tempObject;
    GameObject parentObject;
    GameObject prefabObject;

    [TearDown]
    public void TearDown()
    {
        if (tempObject != null)
        {
            Object.DestroyImmediate(tempObject);
        }
        if (parentObject != null)
        {
            Object.DestroyImmediate(parentObject);
        }
        if (prefabObject != null)
        {
            Object.DestroyImmediate(prefabObject);
        }
    }

    [Test]
    public void MinimapIsOptInSoLevelMapDoesNotRenderByDefault()
    {
        FieldInfo field = typeof(MinimapUI).GetField("showMinimap", BindingFlags.Public | BindingFlags.Instance);
        Assert.That(field, Is.Not.Null, "MinimapUI should expose showMinimap so the level map can stay disabled.");

        tempObject = new GameObject("MinimapUITest");
        MinimapUI minimap = tempObject.AddComponent<MinimapUI>();

        Assert.That(field.GetValue(minimap), Is.False);
    }

    [Test]
    public void DungeonBuilderSupportsRealLootChestPrefab()
    {
        FieldInfo field = typeof(DungeonBuilder).GetField("lootChestPrefab", BindingFlags.Public | BindingFlags.Instance);
        Assert.That(field, Is.Not.Null, "Loot chests should use a readable chest prefab instead of a plain cube.");
    }

    [Test]
    public void LootChestPrefabIsIgnoredByNavMeshBuild()
    {
        tempObject = new GameObject("DungeonBuilderTest");
        DungeonBuilder builder = tempObject.AddComponent<DungeonBuilder>();

        parentObject = new GameObject("Dungeon");
        SetPrivateField(builder, "dungeonParent", parentObject.transform);

        prefabObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        prefabObject.name = "ReadableChestVisual";
        builder.lootChestPrefab = prefabObject;

        Invoke(builder, "CreateLootChest", Vector3.zero);

        Transform chest = parentObject.transform.Find("LootChest");
        Assert.That(chest, Is.Not.Null);
        NavMeshModifier modifier = chest.GetComponent<NavMeshModifier>();
        Assert.That(modifier, Is.Not.Null);
        Assert.That(modifier.ignoreFromBuild, Is.True);
    }

    [Test]
    public void TankDragonPrefabIsBiggerButStaysInsideDungeonScale()
    {
        GameObject tank = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Tank.prefab");
        Assert.That(tank, Is.Not.Null);

        Bounds bounds = GetRendererBounds(tank);
        Assert.That(bounds.size.x, Is.InRange(1.45f, 1.95f), "Dragon should read as a large enemy in the room.");
        Assert.That(bounds.size.z, Is.InRange(1.45f, 1.95f), "Dragon should stay narrow enough for 3-cell corridors.");
        Assert.That(bounds.size.y, Is.LessThan(2.05f), "Dragon should stay below the dungeon ceiling.");

        NavMeshAgent agent = tank.GetComponent<NavMeshAgent>();
        Assert.That(agent, Is.Not.Null);
        Assert.That(agent.radius, Is.InRange(0.62f, 0.8f), "Agent radius should keep the larger dragon from clipping through walls.");
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
}
