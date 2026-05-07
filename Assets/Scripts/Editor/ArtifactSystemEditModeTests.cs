using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class ArtifactSystemEditModeTests
{
    GameObject gameObject;

    [TearDown]
    public void TearDown()
    {
        if (gameObject != null)
        {
            UnityEngine.Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void ArtifactChoicesContainNoDuplicateIds()
    {
        object artifactSystem = CreateArtifactSystem();
        object[] choices = InvokeObjectArray(artifactSystem, "GetChoices", 3);

        Assert.That(choices.Length, Is.EqualTo(3));
        Assert.That(GetString(choices[0], "id"), Is.Not.Empty);
        Assert.That(GetString(choices[0], "id"), Is.Not.EqualTo(GetString(choices[1], "id")));
        Assert.That(GetString(choices[0], "id"), Is.Not.EqualTo(GetString(choices[2], "id")));
        Assert.That(GetString(choices[1], "id"), Is.Not.EqualTo(GetString(choices[2], "id")));
    }

    [Test]
    public void ApplyingArtifactsChangesStatsAndActivatesSynergyOnce()
    {
        object artifactSystem = CreateArtifactSystem();
        PlayerStats stats = gameObject.AddComponent<PlayerStats>();
        stats.ResetStats();

        object flameOne = FindArtifact(artifactSystem, "ember_core");
        object flameTwo = FindArtifact(artifactSystem, "cinder_lens");

        Invoke(artifactSystem, "ApplyArtifact", flameOne, stats);
        Assert.That(stats.damageMultiplier, Is.GreaterThan(1f));
        float afterFirst = stats.damageMultiplier;

        Invoke(artifactSystem, "ApplyArtifact", flameTwo, stats);
        Assert.That(stats.damageMultiplier, Is.GreaterThan(afterFirst));
        float afterSecond = stats.damageMultiplier;

        Invoke(artifactSystem, "ApplyArtifact", flameTwo, stats);
        Assert.That(stats.damageMultiplier, Is.EqualTo(afterSecond).Within(0.001f));
    }

    [Test]
    public void ResetStatsClearsArtifactInventory()
    {
        object artifactSystem = CreateArtifactSystem();
        PlayerStats stats = gameObject.AddComponent<PlayerStats>();
        stats.ResetStats();

        Invoke(artifactSystem, "ApplyArtifact", FindArtifact(artifactSystem, "ember_core"), stats);
        Assert.That(GetInventoryCount(stats), Is.EqualTo(1));

        stats.ResetStats();
        Assert.That(GetInventoryCount(stats), Is.EqualTo(0));
    }

    object CreateArtifactSystem()
    {
        Type type = Type.GetType("ArtifactSystem, Assembly-CSharp");
        Assert.That(type, Is.Not.Null, "ArtifactSystem type should exist in Assembly-CSharp.");

        gameObject = new GameObject("ArtifactSystemTest");
        object component = gameObject.AddComponent(type);
        Invoke(component, "Awake");
        return component;
    }

    object FindArtifact(object artifactSystem, string id)
    {
        object[] all = InvokeObjectArray(artifactSystem, "GetAllArtifacts");
        for (int i = 0; i < all.Length; i++)
        {
            if (GetString(all[i], "id") == id)
            {
                return all[i];
            }
        }

        Assert.Fail("Expected artifact id was not found: " + id);
        return null;
    }

    object[] InvokeObjectArray(object target, string methodName, params object[] args)
    {
        object result = Invoke(target, methodName, args);
        IEnumerable enumerable = result as IEnumerable;
        Assert.That(enumerable, Is.Not.Null, methodName + " should return an enumerable.");

        ArrayList list = new ArrayList();
        foreach (object item in enumerable)
        {
            list.Add(item);
        }

        return list.ToArray();
    }

    object Invoke(object target, string methodName, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(method, Is.Not.Null, "Method should exist: " + methodName);
        return method.Invoke(target, args);
    }

    string GetString(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName);
        Assert.That(field, Is.Not.Null, "Field should exist: " + fieldName);
        return (string)field.GetValue(target);
    }

    int GetInventoryCount(PlayerStats stats)
    {
        FieldInfo field = typeof(PlayerStats).GetField("artifactInventory");
        Assert.That(field, Is.Not.Null, "PlayerStats.artifactInventory should exist.");
        object inventory = field.GetValue(stats);
        Assert.That(inventory, Is.Not.Null, "artifactInventory should be initialized.");

        PropertyInfo countProperty = inventory.GetType().GetProperty("Count");
        Assert.That(countProperty, Is.Not.Null, "ArtifactInventory.Count should exist.");
        return (int)countProperty.GetValue(inventory);
    }
}
