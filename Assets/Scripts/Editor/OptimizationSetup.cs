using UnityEngine;
using UnityEditor;

public class OptimizationSetup
{
    public static void ApplyAll()
    {
        EnableStaticBatching();
        MarkDungeonObjectsStatic();
        SetupOcclusionCulling();
        EnableGPUInstancing();
        Debug.Log("All optimizations applied!");
    }

    static void EnableStaticBatching()
    {
        PlayerSettings.SetStaticBatchingForPlatform(BuildTarget.StandaloneWindows64, true);
        PlayerSettings.SetStaticBatchingForPlatform(BuildTarget.StandaloneOSX, true);
        PlayerSettings.SetStaticBatchingForPlatform(BuildTarget.StandaloneLinux64, true);

        PlayerSettings.SetDynamicBatchingForPlatform(BuildTarget.StandaloneWindows64, true);
        PlayerSettings.SetDynamicBatchingForPlatform(BuildTarget.StandaloneOSX, true);
        PlayerSettings.SetDynamicBatchingForPlatform(BuildTarget.StandaloneLinux64, true);

        Debug.Log("Static + Dynamic Batching enabled for desktop platforms.");
    }

    static void MarkDungeonObjectsStatic()
    {
        int count = 0;

        GameObject dungeon = GameObject.Find("Dungeon");
        if (dungeon != null)
        {
            MeshRenderer[] renderers = dungeon.GetComponentsInChildren<MeshRenderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                GameObject obj = renderers[i].gameObject;
                if (!obj.isStatic)
                {
                    obj.isStatic = true;
                    count++;
                }
            }
        }

        MeshRenderer[] allRenderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
        for (int i = 0; i < allRenderers.Length; i++)
        {
            GameObject obj = allRenderers[i].gameObject;
            if (obj.CompareTag("Untagged") && !obj.CompareTag("Player") && !obj.CompareTag("Enemy"))
            {
                if (!obj.isStatic)
                {
                    obj.isStatic = true;
                    count++;
                }
            }
        }

        Debug.Log("Marked " + count + " objects as static for batching.");
    }

    static void SetupOcclusionCulling()
    {
        int count = 0;

        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        for (int i = 0; i < allObjects.Length; i++)
        {
            if (!allObjects[i].isStatic) continue;

            MeshRenderer renderer = allObjects[i].GetComponent<MeshRenderer>();
            if (renderer == null) continue;

            StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(allObjects[i]);
            bool changed = false;

            if ((flags & StaticEditorFlags.OccluderStatic) == 0)
            {
                flags |= StaticEditorFlags.OccluderStatic;
                changed = true;
            }
            if ((flags & StaticEditorFlags.OccludeeStatic) == 0)
            {
                flags |= StaticEditorFlags.OccludeeStatic;
                changed = true;
            }

            if (changed)
            {
                GameObjectUtility.SetStaticEditorFlags(allObjects[i], flags);
                count++;
            }
        }

        Debug.Log("Set Occluder+Occludee flags on " + count + " static objects.");
    }

    static void EnableGPUInstancing()
    {
        string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Materials" });
        int count = 0;

        for (int i = 0; i < materialGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(materialGuids[i]);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat != null && !mat.enableInstancing)
            {
                mat.enableInstancing = true;
                EditorUtility.SetDirty(mat);
                count++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("GPU Instancing enabled on " + count + " materials.");
    }
}
