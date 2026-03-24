using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class LightingSetup
{
    public static void ApplyAllMenuItem()
    {
        LightingSetup instance = new LightingSetup();
        instance.ApplyLightingSettings();
        instance.CreatePostProcessingVolume();
    }

    void ApplyLightingSettings()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.35f, 0.3f, 0.38f);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = new Color(0.08f, 0.07f, 0.1f);
        RenderSettings.fogDensity = 0.012f;

        RenderSettings.skybox = null;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;

        Light dirLight = Object.FindAnyObjectByType<Light>();
        if (dirLight != null && dirLight.type == LightType.Directional)
        {
            dirLight.intensity = 0.8f;
            dirLight.color = new Color(0.55f, 0.55f, 0.65f);
            dirLight.lightmapBakeType = LightmapBakeType.Mixed;
            EditorUtility.SetDirty(dirLight);
        }

        Lightmapping.bakedGI = true;
        Lightmapping.realtimeGI = false;

        Debug.Log("Dungeon lighting settings applied.");
    }

    void CreatePostProcessingVolume()
    {
        GameObject existing = GameObject.Find("DungeonVolume");
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        GameObject volumeObj = new GameObject("DungeonVolume");
        Volume volume = volumeObj.AddComponent<Volume>();
        volume.isGlobal = true;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();

        Bloom bloom = profile.Add<Bloom>(true);
        bloom.threshold.value = 0.9f;
        bloom.intensity.value = 0.5f;

        Vignette vignette = profile.Add<Vignette>(true);
        vignette.intensity.value = 0.3f;
        vignette.color.value = new Color(0.05f, 0f, 0.05f);

        ColorAdjustments colorAdj = profile.Add<ColorAdjustments>(true);
        colorAdj.postExposure.value = 0f;
        colorAdj.contrast.value = 10f;
        colorAdj.saturation.value = -5f;

        EnsureDirectory("Assets/Settings");
        string profilePath = "Assets/Settings/DungeonVolumeProfile.asset";
        AssetDatabase.CreateAsset(profile, profilePath);
        volume.profile = profile;

        Debug.Log("Post-processing volume created with Bloom, Vignette, and Color Adjustments.");
    }

    void EnsureDirectory(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            string folder = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
