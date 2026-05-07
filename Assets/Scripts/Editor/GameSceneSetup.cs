using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using Unity.Cinemachine;

public class GameSceneSetup : EditorWindow
{
    const string PENDING_BUILD_KEY = "GameSceneSetup_PendingBuild";

    [MenuItem("Tools/Build Game")]
    public static void BuildGame()
    {
        bool fbxChanged = ConfigureAllFBXImports();
        if (fbxChanged)
        {
            EditorPrefs.SetBool(PENDING_BUILD_KEY, true);
            Debug.Log("[Build Game] FBX reimported — scene will build automatically after reload.");
        }
        else
        {
            RunFullBuild();
        }
    }

    [UnityEditor.Callbacks.DidReloadScripts]
    static void OnScriptsReloaded()
    {
        if (EditorPrefs.GetBool(PENDING_BUILD_KEY, false))
        {
            EditorPrefs.DeleteKey(PENDING_BUILD_KEY);
            EditorApplication.delayCall += RunFullBuild;
        }
    }

    static void RunFullBuild()
    {
        GameSceneSetup window = CreateInstance<GameSceneSetup>();
        window.CleanUpScene();
        window.CreateMaterials();
        window.CreateAllEnemyPrefabs();
        window.CreateEnemyAnimatorControllers();
        window.CreateProjectilePrefab();
        window.CreateTorchPrefab();
        window.CreateExplosionPrefab();
        window.CreateGameScene();
        window.AutoAssignAll();
        DestroyImmediate(window);
        LightingSetup.ApplyAllMenuItem();
        OptimizationSetup.ApplyAll();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Build Game] Done! Scene built, lighting applied, optimizations set, scene saved.");
    }

    static bool ConfigureAllFBXImports()
    {
        string[] fbxFiles = { "Skeleton", "Slime", "Dragon" };
        string fbxDir = "Assets/ExternalAssets/Monsters/FBX/";
        bool anyChanged = ConfigureBrokenVectorDungeonAssets();
        anyChanged |= ConfigureKenneyUIAssets();
        anyChanged |= ConfigureKayKitWeaponAssets();

        for (int i = 0; i < fbxFiles.Length; i++)
        {
            string name = fbxFiles[i];
            string fbxPath = fbxDir + name + ".fbx";
            ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null) continue;

            bool changed = false;

            if (importer.animationType != ModelImporterAnimationType.Generic)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                changed = true;
            }

            if (!importer.importAnimation)
            {
                importer.importAnimation = true;
                changed = true;
            }

            float wantScale = name == "Dragon" ? 0.34f : 0.46f;
            if (Mathf.Abs(importer.globalScale - wantScale) > 0.01f)
            {
                importer.globalScale = wantScale;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
                anyChanged = true;
                Debug.Log("[Build Game] Configured FBX: " + name);
            }
        }

        AssetDatabase.Refresh();
        return anyChanged;
    }

    static bool ConfigureKayKitWeaponAssets()
    {
        bool anyChanged = false;
        string root = "Assets/ExternalAssets/KayKitFantasyWeaponsBits";
        if (!System.IO.Directory.Exists(root)) return false;

        string[] modelPaths = System.IO.Directory.GetFiles(root, "*.fbx", System.IO.SearchOption.AllDirectories);
        for (int i = 0; i < modelPaths.Length; i++)
        {
            string modelPath = modelPaths[i].Replace('\\', '/');
            ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer == null) continue;

            bool changed = false;
            if (importer.importAnimation)
            {
                importer.importAnimation = false;
                changed = true;
            }

            if (importer.materialLocation == ModelImporterMaterialLocation.External)
            {
                importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
                changed = true;
            }

            if (Mathf.Abs(importer.globalScale - 1f) > 0.001f)
            {
                importer.globalScale = 1f;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
                anyChanged = true;
            }
        }

        return anyChanged;
    }

    static bool ConfigureKenneyUIAssets()
    {
        bool anyChanged = false;
        string uiRoot = "Assets/ExternalAssets/KenneyUIPack/PNG";
        if (!System.IO.Directory.Exists(uiRoot)) return false;

        string[] pngPaths = System.IO.Directory.GetFiles(uiRoot, "*.png", System.IO.SearchOption.AllDirectories);
        for (int i = 0; i < pngPaths.Length; i++)
        {
            string path = pngPaths[i].Replace('\\', '/');
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            if (path.Contains("button_rectangle") || path.Contains("input_rectangle"))
            {
                Vector4 wantedBorder = new Vector4(14f, 14f, 14f, 14f);
                if (importer.spriteBorder != wantedBorder)
                {
                    importer.spriteBorder = wantedBorder;
                    changed = true;
                }
            }

            if (changed)
            {
                importer.SaveAndReimport();
                anyChanged = true;
            }
        }

        return anyChanged;
    }

    static bool ConfigureBrokenVectorDungeonAssets()
    {
        bool anyChanged = false;
        string[] readableModelPathList =
        {
            "Assets/ExternalAssets/BrokenVectorDungeon/Models/Tiles/Dungeon_Wall_Var1.fbx",
            "Assets/ExternalAssets/BrokenVectorDungeon/Models/Tiles/Dungeon_Wall_Var2.fbx",
            "Assets/ExternalAssets/BrokenVectorDungeon/Models/Tiles/Dungeon_Wall_Var3.fbx",
            "Assets/ExternalAssets/BrokenVectorDungeon/Models/Lamps/Torch_Wall.fbx"
        };

        HashSet<string> readableModelPaths = new HashSet<string>(readableModelPathList);
        string[] allModelPaths = System.IO.Directory.GetFiles(
            "Assets/ExternalAssets/BrokenVectorDungeon/Models",
            "*.fbx",
            System.IO.SearchOption.AllDirectories);

        for (int i = 0; i < allModelPaths.Length; i++)
        {
            string modelPath = allModelPaths[i].Replace('\\', '/');
            ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer == null) continue;

            bool importerChanged = false;
            if (readableModelPaths.Contains(modelPath) && !importer.isReadable)
            {
                importer.isReadable = true;
                importerChanged = true;
                Debug.Log("[Build Game] Enabled readable mesh: " + modelPath);
            }

            if (importer.materialLocation == ModelImporterMaterialLocation.External)
            {
                importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
                importerChanged = true;
                Debug.Log("[Build Game] Moved model materials into prefab import data: " + modelPath);
            }

            if (importerChanged)
            {
                importer.SaveAndReimport();
                anyChanged = true;
            }
        }

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogWarning("[Build Game] URP Lit shader not found; BrokenVectorDungeon materials were not converted.");
            return anyChanged;
        }

        string[] materialGuids = AssetDatabase.FindAssets(
            "t:Material",
            new[] { "Assets/ExternalAssets/BrokenVectorDungeon/Materials" });

        for (int i = 0; i < materialGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(materialGuids[i]);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null || material.shader == urpLit) continue;

            Texture albedo = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
            Color color = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
            Color emission = material.HasProperty("_EmissionColor") ? material.GetColor("_EmissionColor") : Color.black;
            float metallic = material.HasProperty("_Metallic") ? material.GetFloat("_Metallic") : 0f;
            float smoothness = material.HasProperty("_Glossiness") ? material.GetFloat("_Glossiness") : 0.2f;

            material.shader = urpLit;

            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", albedo);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", emission);
            if (emission.maxColorComponent > 0.001f)
            {
                material.EnableKeyword("_EMISSION");
            }

            EditorUtility.SetDirty(material);
            anyChanged = true;
            Debug.Log("[Build Game] Converted material to URP Lit: " + path);
        }

        if (anyChanged)
        {
            AssetDatabase.SaveAssets();
        }

        return anyChanged;
    }

    void AutoAssignAll()
    {
        // Load materials
        Material floorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/FloorMaterial.mat");
        Material wallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/ExternalAssets/BrokenVectorDungeon/Materials/Stone_Wall.mat");
        if (wallMat == null)
        {
            wallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/WallMaterial.mat");
        }
        Material ceilingMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/CeilingMaterial.mat");
        Material exitMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ExitMaterial.mat");
        Material sealMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SealMaterial.mat");

        // Load prefabs
        GameObject gruntPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Grunt.prefab");
        GameObject rangerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Ranger.prefab");
        GameObject tankPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Tank.prefab");
        GameObject projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Combat/Projectile.prefab");
        GameObject torchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalAssets/BrokenVectorDungeon/Prefabs/Lamps/Torch_Wall.prefab");
        if (torchPrefab == null)
        {
            torchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Dungeon/Torch.prefab");
        }
        GameObject[] wallPrefabs =
        {
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalAssets/BrokenVectorDungeon/Prefabs/Tiles/Dungeon_Wall_Var1.prefab"),
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalAssets/BrokenVectorDungeon/Prefabs/Tiles/Dungeon_Wall_Var2.prefab"),
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalAssets/BrokenVectorDungeon/Prefabs/Tiles/Dungeon_Wall_Var3.prefab")
        };
        GameObject explosionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VFX/Explosion.prefab");

        // Load input actions
        UnityEngine.InputSystem.InputActionAsset inputActions =
            AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/InputSystem_Actions.inputactions");

        // Assign to GameManager
        GameObject gmObj = GameObject.Find("GameManager");
        if (gmObj != null)
        {
            DungeonBuilder builder = gmObj.GetComponent<DungeonBuilder>();
            if (builder != null)
            {
                builder.floorMaterial = floorMat;
                builder.wallMaterial = wallMat;
                builder.ceilingMaterial = ceilingMat;
                builder.exitMaterial = exitMat;
                builder.torchPrefab = torchPrefab;
                builder.wallPrefabs = wallPrefabs;
                builder.wallVisualOverlap = 0.08f;
                builder.wallCollisionOverlap = 0.16f;
                AudioClip chestSfx = AssetDatabase.LoadAssetAtPath<AudioClip>(
                    "Assets/ExternalAssets/KenneyImpact/Audio/impactMetal_light_003.ogg");
                if (chestSfx != null)
                {
                    builder.chestOpenClip = chestSfx;
                }
                EditorUtility.SetDirty(builder);
            }

            DungeonDecorLayer decor = gmObj.GetComponent<DungeonDecorLayer>();
            if (decor != null)
            {
                AssignDecorPrefabs(decor);
                if (builder != null) builder.decorLayer = decor;
                EditorUtility.SetDirty(decor);
            }

            DungeonGenerator generator = gmObj.GetComponent<DungeonGenerator>();
            if (generator != null)
            {
                generator.gridWidth = 72;
                generator.gridHeight = 72;
                generator.maxRooms = 10;
                generator.roomMinSize = 6;
                generator.roomMaxSize = 10;
                generator.roomPadding = 3;
                EditorUtility.SetDirty(generator);
            }

            EnemySpawner spawner = gmObj.GetComponent<EnemySpawner>();
            if (spawner != null)
            {
                spawner.gruntPrefab = gruntPrefab;
                spawner.rangerPrefab = rangerPrefab;
                spawner.tankPrefab = tankPrefab;
                EditorUtility.SetDirty(spawner);
            }

            RoomSealManager sealMgr = gmObj.GetComponent<RoomSealManager>();
            if (sealMgr != null && sealMat != null)
            {
                sealMgr.sealMaterial = sealMat;
                EditorUtility.SetDirty(sealMgr);
            }

            LevelManager levelMgr = gmObj.GetComponent<LevelManager>();
            GameManager gm = gmObj.GetComponent<GameManager>();

            if (gm != null && levelMgr != null)
            {
                gm.levelManager = levelMgr;
                EditorUtility.SetDirty(gm);
            }

            GameUI gameUI = Object.FindAnyObjectByType<GameUI>();
            if (gameUI != null)
            {
                PerkSystem perkSys = gmObj.GetComponent<PerkSystem>();
                if (perkSys != null) gameUI.perkSystem = perkSys;

                PlayerStats stats = gmObj.GetComponent<PlayerStats>();
                if (stats != null) gameUI.playerStatsRef = stats;

                EditorUtility.SetDirty(gameUI);
            }
        }

        // Assign to Player
        GameObject playerObj = GameObject.Find("Player");
        if (playerObj != null)
        {
            ProjectileShooter shooter = playerObj.GetComponent<ProjectileShooter>();
            if (shooter != null)
            {
                shooter.projectilePrefab = projectilePrefab;
                EditorUtility.SetDirty(shooter);
            }

            PlayerController playerController = playerObj.GetComponent<PlayerController>();
            Transform cameraHolder = null;
            if (playerController != null)
            {
                cameraHolder = playerController.cameraHolder;
            }
            if (cameraHolder == null)
            {
                Transform foundHolder = playerObj.transform.Find("CameraHolder");
                if (foundHolder != null) cameraHolder = foundHolder;
            }

            if (playerController != null && cameraHolder != null)
            {
                WeaponViewModelController viewModel = cameraHolder.GetComponentInChildren<WeaponViewModelController>(true);
                if (viewModel == null)
                {
                    GameObject createdViewModelRoot = new GameObject("WeaponViewModel");
                    createdViewModelRoot.transform.SetParent(cameraHolder, false);
                    viewModel = createdViewModelRoot.AddComponent<WeaponViewModelController>();
                }

                Transform viewModelRoot = viewModel.transform;
                if (viewModel.swordModel == null)
                {
                    viewModel.swordModel = CreateWeaponViewModelChild(viewModelRoot, "SwordModel",
                        "Assets/ExternalAssets/KayKitFantasyWeaponsBits/KayKit_FantasyWeaponsBits_1.0_FREE/Assets/fbx(unity)/sword_D.fbx",
                        Vector3.zero, new Vector3(74f, -18f, -12f), 0.78f);
                }
                if (viewModel.axeModel == null)
                {
                    viewModel.axeModel = CreateWeaponViewModelChild(viewModelRoot, "AxeModel",
                        "Assets/ExternalAssets/KayKitFantasyWeaponsBits/KayKit_FantasyWeaponsBits_1.0_FREE/Assets/fbx(unity)/axe_C.fbx",
                        Vector3.zero, new Vector3(72f, -20f, -15f), 0.82f);
                }
                if (viewModel.spearModel == null)
                {
                    viewModel.spearModel = CreateWeaponViewModelChild(viewModelRoot, "SpearModel",
                        "Assets/ExternalAssets/KayKitFantasyWeaponsBits/KayKit_FantasyWeaponsBits_1.0_FREE/Assets/fbx(unity)/spear_A.fbx",
                        new Vector3(0f, -0.06f, 0.06f), new Vector3(82f, -12f, -8f), 0.95f);
                }
                if (viewModel.hammerModel == null)
                {
                    viewModel.hammerModel = CreateWeaponViewModelChild(viewModelRoot, "HammerModel",
                        "Assets/ExternalAssets/KayKitFantasyWeaponsBits/KayKit_FantasyWeaponsBits_1.0_FREE/Assets/fbx(unity)/hammer_B.fbx",
                        Vector3.zero, new Vector3(72f, -22f, -18f), 0.84f);
                }
                if (viewModel.bowModel == null)
                {
                    viewModel.bowModel = CreateWeaponViewModelChild(viewModelRoot, "BowModel",
                        "Assets/ExternalAssets/KayKitFantasyWeaponsBits/KayKit_FantasyWeaponsBits_1.0_FREE/Assets/fbx(unity)/bow_A_withString.fbx",
                        new Vector3(-0.04f, -0.03f, 0.1f), new Vector3(66f, -14f, -88f), 0.92f);
                }
                ConfigureWeaponViewModel(viewModel);
                viewModel.SetMode(MeleeWeaponMode.Sword);

                MeleeWeaponController melee = playerObj.GetComponent<MeleeWeaponController>();
                if (melee == null)
                {
                    melee = playerObj.AddComponent<MeleeWeaponController>();
                }
                melee.attackOrigin = cameraHolder;
                melee.viewModel = viewModel;
                melee.rangedShooter = playerObj.GetComponent<ProjectileShooter>();
                PlayerStats stats = gmObj != null ? gmObj.GetComponent<PlayerStats>() : null;
                if (stats != null) melee.damageMultiplierSource = stats;
                playerController.meleeWeapon = melee;
                EditorUtility.SetDirty(viewModel);
                EditorUtility.SetDirty(melee);
                EditorUtility.SetDirty(playerController);
            }

            UnityEngine.InputSystem.PlayerInput playerInput =
                playerObj.GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (playerInput != null && inputActions != null)
            {
                playerInput.actions = inputActions;
                EditorUtility.SetDirty(playerInput);
            }

            GameUI uiRef = Object.FindAnyObjectByType<GameUI>();
            if (uiRef != null)
            {
                PlayerController pcRef = playerObj.GetComponent<PlayerController>();
                HealthSystem hsRef = playerObj.GetComponent<HealthSystem>();
                if (pcRef != null) uiRef.playerRef = pcRef;
                if (hsRef != null) uiRef.playerHealthRef = hsRef;
                EditorUtility.SetDirty(uiRef);
            }

            if (gmObj != null)
            {
                LevelManager lm = gmObj.GetComponent<LevelManager>();
                GameManager gmRef = gmObj.GetComponent<GameManager>();
                PlayerController pcComp = playerObj.GetComponent<PlayerController>();
                HealthSystem hsComp = playerObj.GetComponent<HealthSystem>();
                if (lm != null && pcComp != null)
                {
                    lm.player = pcComp;
                    lm.playerHealth = hsComp;
                    EditorUtility.SetDirty(lm);
                }
                if (gmRef != null && pcComp != null)
                {
                    gmRef.player = pcComp;
                    gmRef.playerHealth = hsComp;
                    EditorUtility.SetDirty(gmRef);
                }
            }
        }

        // Assign explosion to projectile prefab
        if (projectilePrefab != null && explosionPrefab != null)
        {
            Projectile proj = projectilePrefab.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.explosionPrefab = explosionPrefab;
                EditorUtility.SetDirty(proj);
                AssetDatabase.SaveAssets();
            }
        }

        // Wire audio clips from Kenney packs
        AudioClip impactHit = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ExternalAssets/KenneyImpact/Audio/impactWood_heavy_002.ogg");
        AudioClip impactEnemyHit = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ExternalAssets/KenneyImpact/Audio/impactPunch_heavy_004.ogg");
        AudioClip dashSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ExternalAssets/KenneyImpact/Audio/impactSoft_medium_000.ogg");

        // Wire HitEffectSpawner on projectile prefab
        if (projectilePrefab != null)
        {
            HitEffectSpawner hitSpawner = projectilePrefab.GetComponent<HitEffectSpawner>();
            if (hitSpawner != null)
            {
                SerializedObject hitSO = new SerializedObject(hitSpawner);
                SerializedProperty hitVFXProp = hitSO.FindProperty("hitVFXPrefab");
                SerializedProperty hitSFXProp = hitSO.FindProperty("hitSFX");
                SerializedProperty hitEnemySFXProp = hitSO.FindProperty("hitEnemySFX");
                if (hitVFXProp != null) hitVFXProp.objectReferenceValue = explosionPrefab;
                if (hitSFXProp != null) hitSFXProp.objectReferenceValue = impactHit;
                if (hitEnemySFXProp != null) hitEnemySFXProp.objectReferenceValue = impactEnemyHit;
                hitSO.ApplyModifiedProperties();
                EditorUtility.SetDirty(hitSpawner);
                AssetDatabase.SaveAssets();
            }
        }

        // Wire DashVFX on Player
        if (playerObj != null)
        {
            DashVFX dashVfx = playerObj.GetComponent<DashVFX>();
            if (dashVfx != null)
            {
                SerializedObject dashSO = new SerializedObject(dashVfx);
                SerializedProperty dashSFXProp = dashSO.FindProperty("dashSFX");
                if (dashSFXProp != null && dashSound != null) dashSFXProp.objectReferenceValue = dashSound;

                ParticleSystem dashTrailPS = null;
                Transform dashTrailT = playerObj.transform.Find("DashTrailParticles");
                if (dashTrailT != null) dashTrailPS = dashTrailT.GetComponent<ParticleSystem>();
                SerializedProperty dashTrailProp = dashSO.FindProperty("dashTrailParticles");
                if (dashTrailProp != null && dashTrailPS != null) dashTrailProp.objectReferenceValue = dashTrailPS;

                dashSO.ApplyModifiedProperties();
                EditorUtility.SetDirty(dashVfx);
            }

            PlayerController playerPC = playerObj.GetComponent<PlayerController>();
            if (playerPC != null && dashVfx != null)
            {
                SerializedObject pcSO = new SerializedObject(playerPC);
                SerializedProperty dashVFXProp = pcSO.FindProperty("dashVFX");
                if (dashVFXProp != null) dashVFXProp.objectReferenceValue = dashVfx;
                pcSO.ApplyModifiedProperties();
                EditorUtility.SetDirty(playerPC);
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("All materials and prefabs auto-assigned!");
    }

    void CleanUpScene()
    {
        string[] namesToDelete = new string[]
        {
            "GameManager", "Player", "GameCanvas", "EventSystem",
            "DungeonVolume", "Dungeon", "Global Volume", "Main Camera"
        };

        for (int n = 0; n < namesToDelete.Length; n++)
        {
            GameObject[] found = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] != null && found[i].name == namesToDelete[n])
                {
                    DestroyImmediate(found[i]);
                }
            }
        }

        Debug.Log("Scene cleaned up. You can now Create Game Scene Objects.");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    void CreateGameScene()
    {
        // --- GameManager (thin state machine) ---
        GameObject gmObj = new GameObject("GameManager");
        GameManager gm = gmObj.AddComponent<GameManager>();

        DungeonGenerator gen = gmObj.AddComponent<DungeonGenerator>();
        DungeonBuilder builder = gmObj.AddComponent<DungeonBuilder>();
        EnemySpawner spawner = gmObj.AddComponent<EnemySpawner>();
        PlayerStats playerStats = gmObj.AddComponent<PlayerStats>();
        PerkSystem perkSystem = gmObj.AddComponent<PerkSystem>();
        ArtifactSystem artifactSystem = gmObj.AddComponent<ArtifactSystem>();
        RoomSealManager sealManager = gmObj.AddComponent<RoomSealManager>();
        AudioManager audioManager = gmObj.AddComponent<AudioManager>();
        AssignAudioManagerClips(audioManager);

        LevelManager levelMgr = gmObj.AddComponent<LevelManager>();
        levelMgr.dungeonGenerator = gen;
        levelMgr.dungeonBuilder = builder;
        levelMgr.enemySpawner = spawner;
        levelMgr.roomSealManager = sealManager;
        levelMgr.playerStats = playerStats;
        gm.levelManager = levelMgr;

        gmObj.AddComponent<CombatTextManager>();

        builder.generator = gen;
        DungeonDecorLayer decorLayer = gmObj.AddComponent<DungeonDecorLayer>();
        AssignDecorPrefabs(decorLayer);
        builder.decorLayer = decorLayer;
        builder.tileSize = 0.85f;
        builder.wallHeight = 2.5f;
        builder.wallVisualOverlap = 0.08f;
        builder.wallCollisionOverlap = 0.16f;
        gen.gridWidth = 72;
        gen.gridHeight = 72;
        gen.roomMinSize = 6;
        gen.roomMaxSize = 10;
        gen.maxRooms = 10;
        gen.roomPadding = 3;

        builder.chestOpenClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/ExternalAssets/KenneyImpact/Audio/impactMetal_light_003.ogg");
        builder.lootChestPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/ExternalAssets/BrokenVectorDungeon/Prefabs/Furniture/Chest.prefab");

        // --- Player ---
        GameObject playerObj = new GameObject("Player");
        playerObj.tag = "Player";
        playerObj.layer = LayerMask.NameToLayer("Default");

        CharacterController cc = playerObj.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.4f;
        cc.center = new Vector3(0, 1f, 0);

        PlayerController pc = playerObj.AddComponent<PlayerController>();
        HealthSystem playerHealth = playerObj.AddComponent<HealthSystem>();
        playerHealth.maxHealth = 100;

        gm.player = pc;
        gm.playerHealth = playerHealth;
        levelMgr.player = pc;
        levelMgr.playerHealth = playerHealth;

        DashVFX dashVFX = playerObj.AddComponent<DashVFX>();

        GameObject dashParticlesObj = new GameObject("DashTrailParticles");
        dashParticlesObj.transform.SetParent(playerObj.transform);
        dashParticlesObj.transform.localPosition = new Vector3(0, 0.5f, -0.3f);
        ParticleSystem dashPS = dashParticlesObj.AddComponent<ParticleSystem>();
        var dashMain = dashPS.main;
        dashMain.startLifetime = 0.3f;
        dashMain.startSpeed = 2f;
        dashMain.startSize = 0.1f;
        dashMain.startColor = new Color(0.5f, 0.8f, 1f, 0.6f);
        dashMain.maxParticles = 30;
        dashMain.loop = true;
        dashMain.simulationSpace = ParticleSystemSimulationSpace.World;
        dashMain.playOnAwake = false;
        var dashEmission = dashPS.emission;
        dashEmission.rateOverTime = 40;
        var dashShape = dashPS.shape;
        dashShape.shapeType = ParticleSystemShapeType.Cone;
        dashShape.angle = 25f;
        dashShape.radius = 0.1f;
        var dashRenderer = dashPS.GetComponent<ParticleSystemRenderer>();
        dashRenderer.material = CreateURPParticleMaterial(new Color(0.5f, 0.8f, 1f, 0.6f), "DashTrail");
        dashPS.Stop();

        UnityEngine.InputSystem.PlayerInput playerInput = playerObj.AddComponent<UnityEngine.InputSystem.PlayerInput>();

        GameObject cameraHolder = new GameObject("CameraHolder");
        cameraHolder.transform.SetParent(playerObj.transform);
        cameraHolder.transform.localPosition = new Vector3(0, 1.6f, 0);
        pc.cameraHolder = cameraHolder.transform;
        cameraHolder.AddComponent<KillCamShake>();

        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            mainCam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }
        mainCam.transform.SetParent(null);
        mainCam.transform.position = Vector3.zero;
        mainCam.transform.rotation = Quaternion.identity;

        if (mainCam.GetComponent<CinemachineBrain>() == null)
        {
            mainCam.gameObject.AddComponent<CinemachineBrain>();
        }

        FirstPersonCameraFollower cameraFollower = mainCam.GetComponent<FirstPersonCameraFollower>();
        if (cameraFollower == null)
        {
            cameraFollower = mainCam.gameObject.AddComponent<FirstPersonCameraFollower>();
        }
        cameraFollower.target = cameraHolder.transform;

        GameObject cmCamObj = new GameObject("CinemachineCamera");
        cmCamObj.transform.SetParent(cameraHolder.transform);
        cmCamObj.transform.localPosition = Vector3.zero;
        cmCamObj.transform.localRotation = Quaternion.identity;

        CinemachineCamera cmCam = cmCamObj.AddComponent<CinemachineCamera>();
        cmCamObj.AddComponent<CinemachineImpulseListener>();

        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(cameraHolder.transform);
        firePoint.transform.localPosition = new Vector3(0, 0, 0.5f);

        ProjectileShooter shooter = playerObj.AddComponent<ProjectileShooter>();
        shooter.firePoint = firePoint.transform;
        shooter.muzzleFlash = firePoint.AddComponent<PlayerMuzzleFlash>();
        pc.shooter = shooter;

        GameObject viewModelRoot = new GameObject("WeaponViewModel");
        viewModelRoot.transform.SetParent(cameraHolder.transform);
        WeaponViewModelController viewModel = viewModelRoot.AddComponent<WeaponViewModelController>();
        viewModel.swordModel = CreateWeaponViewModelChild(viewModelRoot.transform, "SwordModel",
            "Assets/ExternalAssets/KayKitFantasyWeaponsBits/KayKit_FantasyWeaponsBits_1.0_FREE/Assets/fbx(unity)/sword_D.fbx",
            new Vector3(0f, 0f, 0f), new Vector3(74f, -18f, -12f), 0.78f);
        viewModel.axeModel = CreateWeaponViewModelChild(viewModelRoot.transform, "AxeModel",
            "Assets/ExternalAssets/KayKitFantasyWeaponsBits/KayKit_FantasyWeaponsBits_1.0_FREE/Assets/fbx(unity)/axe_C.fbx",
            new Vector3(0f, 0f, 0f), new Vector3(72f, -20f, -15f), 0.82f);
        viewModel.spearModel = CreateWeaponViewModelChild(viewModelRoot.transform, "SpearModel",
            "Assets/ExternalAssets/KayKitFantasyWeaponsBits/KayKit_FantasyWeaponsBits_1.0_FREE/Assets/fbx(unity)/spear_A.fbx",
            new Vector3(0f, -0.06f, 0.06f), new Vector3(82f, -12f, -8f), 0.95f);
        viewModel.hammerModel = CreateWeaponViewModelChild(viewModelRoot.transform, "HammerModel",
            "Assets/ExternalAssets/KayKitFantasyWeaponsBits/KayKit_FantasyWeaponsBits_1.0_FREE/Assets/fbx(unity)/hammer_B.fbx",
            new Vector3(0f, 0f, 0f), new Vector3(72f, -22f, -18f), 0.84f);
        viewModel.bowModel = CreateWeaponViewModelChild(viewModelRoot.transform, "BowModel",
            "Assets/ExternalAssets/KayKitFantasyWeaponsBits/KayKit_FantasyWeaponsBits_1.0_FREE/Assets/fbx(unity)/bow_A_withString.fbx",
            new Vector3(-0.04f, -0.03f, 0.1f), new Vector3(66f, -14f, -88f), 0.92f);
        ConfigureWeaponViewModel(viewModel);
        viewModel.SetMode(MeleeWeaponMode.Sword);

        MeleeWeaponController melee = playerObj.AddComponent<MeleeWeaponController>();
        melee.attackOrigin = cameraHolder.transform;
        melee.damageMultiplierSource = playerStats;
        melee.viewModel = viewModel;
        melee.rangedShooter = shooter;
        pc.meleeWeapon = melee;

        GameObject playerBody = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        playerBody.name = "PlayerBody";
        playerBody.transform.SetParent(playerObj.transform);
        playerBody.transform.localPosition = new Vector3(0, 1f, 0);
        DestroyImmediate(playerBody.GetComponent<Collider>());

        playerObj.transform.position = new Vector3(0, 1, 0);

        // --- UI Canvas ---
        GameObject canvasObj = new GameObject("GameCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        GameUI gameUI = canvasObj.AddComponent<GameUI>();
        gameUI.perkSystem = perkSystem;
        gameUI.artifactSystem = artifactSystem;
        gameUI.playerRef = pc;
        gameUI.playerHealthRef = playerHealth;
        gameUI.playerStatsRef = playerStats;
        AssignGameUIAssets(gameUI);

        CreateUIPanel(canvasObj.transform, "MainMenuPanel", gameUI, "mainMenu");
        CreateUIPanel(canvasObj.transform, "HUDPanel", gameUI, "hud");
        CreateUIPanel(canvasObj.transform, "GameOverPanel", gameUI, "gameOver");
        CreateOptionsPanel(canvasObj.transform, gameUI);
        CreatePerkPanel(canvasObj.transform, gameUI);

        CrosshairUI crosshairUI = canvasObj.AddComponent<CrosshairUI>();
        MinimapUI minimapUI = canvasObj.AddComponent<MinimapUI>();
        levelMgr.minimap = minimapUI;

        // --- Event System ---
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // --- Directional Light ---
        Light dirLight = Object.FindAnyObjectByType<Light>();
        if (dirLight == null)
        {
            GameObject lightObj = new GameObject("Directional Light");
            dirLight = lightObj.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.intensity = 0.8f;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0);
        }

        Debug.Log("Game scene setup complete!");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    void AssignGameUIAssets(GameUI gameUI)
    {
        if (gameUI == null) return;

        gameUI.uiFont = AssetDatabase.LoadAssetAtPath<Font>(
            "Assets/ExternalAssets/KenneyUIPack/Font/Kenney Future Narrow.ttf");
        gameUI.panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/ExternalAssets/KenneyUIPack/PNG/Extra/Default/input_rectangle.png");
        gameUI.buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/ExternalAssets/KenneyUIPack/PNG/Yellow/Default/button_rectangle_depth_flat.png");
        gameUI.compassArrowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/ExternalAssets/KenneyUIPack/PNG/Yellow/Default/arrow_basic_s.png");
    }

    void AssignDecorPrefabs(DungeonDecorLayer decor)
    {
        if (decor == null) return;

        decor.floorPropPrefabs = LoadPrefabArray(
            "Assets/ExternalAssets/BrokenVectorDungeon/Prefabs/Furniture/Barrel_Closed.prefab",
            "Assets/ExternalAssets/BrokenVectorDungeon/Prefabs/Furniture/Barrel_Open.prefab",
            "Assets/ExternalAssets/BrokenVectorDungeon/Prefabs/Furniture/Table_Small.prefab",
            "Assets/ExternalAssets/BrokenVectorDungeon/Prefabs/Furniture/Bench.prefab",
            "Assets/ExternalAssets/BrokenVectorDungeon/Prefabs/Furniture/Carpet_Red.prefab");
        decor.wallPropPrefabs = LoadPrefabArray(
            "Assets/ExternalAssets/BrokenVectorDungeon/Prefabs/Furniture/Banner.prefab",
            "Assets/ExternalAssets/BrokenVectorDungeon/Prefabs/Furniture/WeaponRack_Small.prefab",
            "Assets/ExternalAssets/BrokenVectorDungeon/Prefabs/Furniture/Shelf_Wall.prefab");
        decor.lightPrefabs = System.Array.Empty<GameObject>();
        decor.prefabPropChance = 1f;
        decor.usePrimitiveFallback = false;
    }

    GameObject[] LoadPrefabArray(params string[] paths)
    {
        List<GameObject> prefabs = new List<GameObject>();
        for (int i = 0; i < paths.Length; i++)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);
            if (prefab != null)
            {
                prefabs.Add(prefab);
            }
        }

        return prefabs.ToArray();
    }

    void AssignAudioManagerClips(AudioManager audioManager)
    {
        if (audioManager == null) return;

        SerializedObject so = new SerializedObject(audioManager);
        SetAudioClipArray(so, "footstepClips", new string[]
        {
            "Assets/ExternalAssets/KenneyRPG/Audio/footstep00.ogg",
            "Assets/ExternalAssets/KenneyRPG/Audio/footstep01.ogg",
            "Assets/ExternalAssets/KenneyRPG/Audio/footstep02.ogg",
            "Assets/ExternalAssets/KenneyRPG/Audio/footstep03.ogg"
        });

        SetAudioClipArray(so, "coinPickupClips", new string[]
        {
            "Assets/ExternalAssets/KenneyRPG/Audio/handleCoins.ogg",
            "Assets/ExternalAssets/KenneyRPG/Audio/handleCoins2.ogg"
        });

        SetAudioClipArray(so, "damageTakenClips", new string[]
        {
            "Assets/ExternalAssets/KenneyImpact/Audio/impactPunch_heavy_000.ogg",
            "Assets/ExternalAssets/KenneyImpact/Audio/impactPunch_heavy_002.ogg",
            "Assets/ExternalAssets/KenneyImpact/Audio/impactSoft_heavy_000.ogg"
        });

        SetAudioClipArray(so, "uiClickClips", new string[]
        {
            "Assets/ExternalAssets/KenneyRPG/Audio/metalClick.ogg",
            "Assets/ExternalAssets/KenneyRPG/Audio/bookFlip1.ogg"
        });

        SetAudioClipArray(so, "ambientStingerClips", new string[]
        {
            "Assets/ExternalAssets/KenneyRPG/Audio/creak1.ogg",
            "Assets/ExternalAssets/KenneyRPG/Audio/creak2.ogg",
            "Assets/ExternalAssets/KenneyRPG/Audio/doorClose_1.ogg",
            "Assets/ExternalAssets/KenneyRPG/Audio/metalLatch.ogg"
        });
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    void SetAudioClipArray(SerializedObject so, string fieldName, string[] paths)
    {
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop == null) return;

        prop.arraySize = paths.Length;
        for (int i = 0; i < paths.Length; i++)
        {
            prop.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(paths[i]);
        }
    }

    void CreateUIPanel(Transform parent, string panelName, GameUI gameUI, string type)
    {
        GameObject panel = new GameObject(panelName);
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        UnityEngine.UI.Image bg = panel.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0.07f, 0.048f, 0.11f, 0.93f);

        if (type == "mainMenu")
        {
            gameUI.mainMenuPanel = panel;
            CreateUIText(panel.transform, "Title", "DUNGEON CRAWLER", 44, new Vector2(0, 120));
            CreateUIButton(panel.transform, "StartButton", "Start Game", new Vector2(0, 10));
            CreateUIButton(panel.transform, "OptionsButton", "Options", new Vector2(0, -50));
            CreateUIButton(panel.transform, "QuitButton", "Quit", new Vector2(0, -110));
        }
        else if (type == "hud")
        {
            gameUI.hudPanel = panel;
            bg.color = new Color(0, 0, 0, 0);

            GameObject sliderObj = new GameObject("HealthBar");
            sliderObj.transform.SetParent(panel.transform, false);
            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0, 1);
            sliderRect.anchorMax = new Vector2(0, 1);
            sliderRect.pivot = new Vector2(0, 1);
            sliderRect.anchoredPosition = new Vector2(20, -20);
            sliderRect.sizeDelta = new Vector2(240, 24);
            UnityEngine.UI.Slider slider = sliderObj.AddComponent<UnityEngine.UI.Slider>();
            slider.maxValue = 100;
            slider.value = 100;
            gameUI.healthBar = slider;

            // Slider background
            GameObject sliderBg = new GameObject("Background");
            sliderBg.transform.SetParent(sliderObj.transform, false);
            RectTransform bgRect = sliderBg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            UnityEngine.UI.Image bgImg = sliderBg.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = new Color(0.1f, 0.08f, 0.12f, 0.92f);

            // Slider fill area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            UnityEngine.UI.Image fillImg = fill.AddComponent<UnityEngine.UI.Image>();
            fillImg.color = new Color(0.95f, 0.2f, 0.32f, 1f);
            slider.fillRect = fillRect;

            GameObject hpValObj = CreateUIText(panel.transform, "HealthValueText", "100 / 100", 20, new Vector2(20, -44),
                TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, 1));
            RectTransform hpValRt = hpValObj.GetComponent<RectTransform>();
            hpValRt.sizeDelta = new Vector2(220, 26);
            gameUI.healthText = hpValObj.GetComponent<UnityEngine.UI.Text>();

            CreateUIText(panel.transform, "LevelText", "Level: 1", 24, new Vector2(0, -20),
                TextAnchor.UpperRight, new Vector2(1, 1), new Vector2(1, 1));
            gameUI.levelText = panel.transform.Find("LevelText").GetComponent<UnityEngine.UI.Text>();

            GameObject coinsObj = CreateUIText(panel.transform, "CoinsText", "◆ 0", 28, new Vector2(-24, -52),
                TextAnchor.UpperRight, new Vector2(1, 1), new Vector2(1, 1));
            RectTransform coinsRt = coinsObj.GetComponent<RectTransform>();
            coinsRt.sizeDelta = new Vector2(220, 44);
            UnityEngine.UI.Text coinsUi = coinsObj.GetComponent<UnityEngine.UI.Text>();
            coinsUi.color = new Color(1f, 0.82f, 0.2f);
            coinsUi.fontStyle = FontStyle.Bold;
            gameUI.coinsText = coinsUi;

            GameObject dashObj = CreateUIText(panel.transform, "DashCooldown", "DASH: READY [Q]", 18, new Vector2(20, -80),
                TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, 1));
            gameUI.dashCooldownText = dashObj.GetComponent<UnityEngine.UI.Text>();
            gameUI.dashCooldownText.color = Color.cyan;

            GameObject weaponObj = CreateUIText(panel.transform, "WeaponModeText", "Пуля  [1/2]", 16, new Vector2(20, -108),
                TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, 1));
            RectTransform weaponRt = weaponObj.GetComponent<RectTransform>();
            weaponRt.sizeDelta = new Vector2(320, 28);
            UnityEngine.UI.Text weaponTxt = weaponObj.GetComponent<UnityEngine.UI.Text>();
            weaponTxt.color = new Color(0.75f, 0.95f, 1f);
            gameUI.weaponModeText = weaponTxt;

            GameObject sealObj = CreateUIText(panel.transform, "SealNotification", "", 28, new Vector2(0, 100));
            gameUI.sealNotificationText = sealObj.GetComponent<UnityEngine.UI.Text>();
            sealObj.SetActive(false);

            GameObject streakObj = CreateUIText(panel.transform, "KillStreakText", "", 36, new Vector2(0, 150));
            UnityEngine.UI.Text streakTxt = streakObj.GetComponent<UnityEngine.UI.Text>();
            streakTxt.color = new Color(1f, 0.5f, 0.08f);
            streakTxt.fontStyle = FontStyle.Bold;
            streakObj.SetActive(false);

            GameObject canvasForStreak = panel.transform.parent.gameObject;
            KillStreakUI killStreak = canvasForStreak.GetComponent<KillStreakUI>();
            if (killStreak == null)
            {
                killStreak = canvasForStreak.AddComponent<KillStreakUI>();
            }
            killStreak.streakText = streakTxt;
            killStreak.streakBonusClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/ExternalAssets/KenneyImpact/Audio/impactSoft_medium_000.ogg");

            GameObject vignetteObj = new GameObject("LowHPVignette");
            vignetteObj.transform.SetParent(panel.transform, false);
            RectTransform vigRect = vignetteObj.AddComponent<RectTransform>();
            vigRect.anchorMin = Vector2.zero;
            vigRect.anchorMax = Vector2.one;
            vigRect.offsetMin = Vector2.zero;
            vigRect.offsetMax = Vector2.zero;
            UnityEngine.UI.Image vigImg = vignetteObj.AddComponent<UnityEngine.UI.Image>();
            vigImg.color = new Color(0.8f, 0f, 0f, 0f);
            vigImg.raycastTarget = false;
            gameUI.lowHPVignette = vigImg;
            vignetteObj.SetActive(false);

            GameObject compassRow = new GameObject("ExitCompassRow");
            compassRow.transform.SetParent(panel.transform, false);
            RectTransform compassRowRt = compassRow.AddComponent<RectTransform>();
            compassRowRt.anchorMin = new Vector2(1f, 1f);
            compassRowRt.anchorMax = new Vector2(1f, 1f);
            compassRowRt.pivot = new Vector2(1f, 1f);
            compassRowRt.anchoredPosition = new Vector2(-14f, -14f);
            compassRowRt.sizeDelta = new Vector2(100f, 72f);

            GameObject exitLbl = CreateUIText(compassRow.transform, "ExitCompassTitle", "EXIT", 16, new Vector2(0f, -6f),
                TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            RectTransform exitLblRt = exitLbl.GetComponent<RectTransform>();
            exitLblRt.sizeDelta = new Vector2(90f, 22f);
            UnityEngine.UI.Text exitTxt = exitLbl.GetComponent<UnityEngine.UI.Text>();
            exitTxt.color = new Color(0.35f, 1f, 0.55f);
            exitTxt.fontStyle = FontStyle.Bold;

            GameObject arrowObj = new GameObject("ExitCompassArrow");
            arrowObj.transform.SetParent(compassRow.transform, false);
            RectTransform arrowRt = arrowObj.AddComponent<RectTransform>();
            arrowRt.anchorMin = new Vector2(0.5f, 1f);
            arrowRt.anchorMax = new Vector2(0.5f, 1f);
            arrowRt.pivot = new Vector2(0.5f, 1f);
            arrowRt.anchoredPosition = new Vector2(0f, -28f);
            arrowRt.sizeDelta = new Vector2(44f, 44f);
            UnityEngine.UI.Text arrowTxt = arrowObj.AddComponent<UnityEngine.UI.Text>();
            arrowTxt.text = "\u25B2";
            arrowTxt.fontSize = 38;
            arrowTxt.alignment = TextAnchor.MiddleCenter;
            arrowTxt.color = new Color(1f, 0.82f, 0.15f);
            Font compFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (compFont == null)
            {
                compFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            arrowTxt.font = compFont;

            GameObject canvasRoot = panel.transform.parent.gameObject;
            ExitCompassUI compassUi = canvasRoot.GetComponent<ExitCompassUI>();
            if (compassUi == null)
            {
                compassUi = canvasRoot.AddComponent<ExitCompassUI>();
            }
            compassUi.compassPanel = compassRow;
            compassUi.arrowRoot = arrowRt;
            compassRow.SetActive(false);
        }
        else if (type == "gameOver")
        {
            gameUI.gameOverPanel = panel;
            CreateUIText(panel.transform, "GameOverTitle", "GAME OVER", 48, new Vector2(0, 60));

            GameObject levelReached = CreateUIText(panel.transform, "LevelReached", "You reached level 1", 24, new Vector2(0, 0));
            gameUI.gameOverLevelText = levelReached.GetComponent<UnityEngine.UI.Text>();

            CreateUIButton(panel.transform, "RestartButton", "Restart", new Vector2(0, -60));
            CreateUIButton(panel.transform, "QuitButton2", "Quit", new Vector2(0, -120));
        }
    }

    GameObject CreateUIText(Transform parent, string name, string text, int fontSize, Vector2 position,
        TextAnchor alignment = TextAnchor.MiddleCenter,
        Vector2? anchorMin = null, Vector2? anchorMax = null)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin ?? new Vector2(0.5f, 0.5f);
        rect.anchorMax = anchorMax ?? new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(400, 60);

        UnityEngine.UI.Text uiText = obj.AddComponent<UnityEngine.UI.Text>();
        uiText.text = text;
        uiText.fontSize = fontSize;
        uiText.alignment = alignment;
        uiText.color = Color.white;
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        uiText.font = font;

        return obj;
    }

    void CreateUIButton(Transform parent, string name, string label, Vector2 position)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(200, 40);

        UnityEngine.UI.Image img = btnObj.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0.19f, 0.14f, 0.27f, 1f);

        UnityEngine.UI.Button btn = btnObj.AddComponent<UnityEngine.UI.Button>();

        GameObject textObj = CreateUIText(btnObj.transform, "Text", label, 20, Vector2.zero);
    }

    void CreateOptionsPanel(Transform parent, GameUI gameUI)
    {
        GameObject panel = new GameObject("OptionsPanel");
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        UnityEngine.UI.Image bg = panel.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0.07f, 0.048f, 0.11f, 0.93f);

        gameUI.optionsPanel = panel;

        CreateUIText(panel.transform, "OptionsTitle", "OPTIONS", 44, new Vector2(0, 140));

        // Mouse Sensitivity
        CreateUIText(panel.transform, "SensLabel", "Mouse Sensitivity", 22, new Vector2(0, 60));

        GameObject sensSliderObj = CreateSlider(panel.transform, "SensitivitySlider", new Vector2(0, 20), 0.5f, 10f, 2f);
        gameUI.sensitivitySlider = sensSliderObj.GetComponent<UnityEngine.UI.Slider>();

        GameObject sensValueObj = CreateUIText(panel.transform, "SensitivityValue", "2.0", 20, new Vector2(140, 20));
        RectTransform sensValRt = sensValueObj.GetComponent<RectTransform>();
        sensValRt.sizeDelta = new Vector2(72f, 28f);
        UnityEngine.UI.Text sensValTxt = sensValueObj.GetComponent<UnityEngine.UI.Text>();
        sensValTxt.raycastTarget = false;
        gameUI.sensitivityValueText = sensValTxt;

        // Volume
        CreateUIText(panel.transform, "VolLabel", "Volume", 22, new Vector2(0, -40));

        GameObject volSliderObj = CreateSlider(panel.transform, "VolumeSlider", new Vector2(0, -80), 0f, 1f, 1f);
        gameUI.volumeSlider = volSliderObj.GetComponent<UnityEngine.UI.Slider>();

        GameObject volValueObj = CreateUIText(panel.transform, "VolumeValue", "100%", 20, new Vector2(140, -80));
        RectTransform volValRt = volValueObj.GetComponent<RectTransform>();
        volValRt.sizeDelta = new Vector2(72f, 28f);
        UnityEngine.UI.Text volValTxt = volValueObj.GetComponent<UnityEngine.UI.Text>();
        volValTxt.raycastTarget = false;
        gameUI.volumeValueText = volValTxt;

        // Back button
        CreateUIButton(panel.transform, "BackButton", "Back", new Vector2(0, -160));

        panel.SetActive(false);
    }

    void CreatePerkPanel(Transform parent, GameUI gameUI)
    {
        GameObject panel = new GameObject("PerkPanel");
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        UnityEngine.UI.Image bg = panel.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0.06f, 0.04f, 0.1f, 0.94f);

        gameUI.perkPanel = panel;

        GameObject titleObj = CreateUIText(panel.transform, "PerkTitle", "CHOOSE A PERK", 40, new Vector2(0, 140));
        gameUI.perkTitleText = titleObj.GetComponent<UnityEngine.UI.Text>();

        GameObject perkCoinsObj = CreateUIText(panel.transform, "PerkCoinsLine", "Монеты: ◆ 0", 22, new Vector2(0, 95));
        UnityEngine.UI.Text perkCoinsTxt = perkCoinsObj.GetComponent<UnityEngine.UI.Text>();
        perkCoinsTxt.color = new Color(1f, 0.82f, 0.2f);
        perkCoinsTxt.fontStyle = FontStyle.Bold;
        gameUI.perkShopCoinsText = perkCoinsTxt;

        CreateUIButton(panel.transform, "RerollPerksButton", "Реролл перков (8 ◆)", new Vector2(-200, -200));
        CreateUIButton(panel.transform, "BuyHealButton", "Полное лечение (5 ◆)", new Vector2(200, -200));

        gameUI.perkButtons = new UnityEngine.UI.Button[3];
        gameUI.perkButtonTexts = new UnityEngine.UI.Text[3];
        gameUI.perkButtonIconTexts = new UnityEngine.UI.Text[3];

        for (int i = 0; i < 3; i++)
        {
            float xPos = (i - 1) * 220f;

            GameObject btnObj = new GameObject("PerkButton" + i);
            btnObj.transform.SetParent(panel.transform, false);

            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnRect.anchoredPosition = new Vector2(xPos, -20);
            btnRect.sizeDelta = new Vector2(200, 120);

            UnityEngine.UI.Image btnImg = btnObj.AddComponent<UnityEngine.UI.Image>();
            btnImg.color = new Color(0.2f, 0.3f, 0.5f, 1f);

            UnityEngine.UI.Button btn = btnObj.AddComponent<UnityEngine.UI.Button>();
            gameUI.perkButtons[i] = btn;

            GameObject textObj = CreateUIText(btnObj.transform, "PerkText", "Perk " + (i + 1), 16, Vector2.zero);
            gameUI.perkButtonTexts[i] = textObj.GetComponent<UnityEngine.UI.Text>();
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchoredPosition = new Vector2(18f, -4f);
            textRect.sizeDelta = new Vector2(156f, 104f);

            GameObject iconObj = CreateUIText(btnObj.transform, "ArtifactIcon", "*", 28, new Vector2(-72f, 34f));
            UnityEngine.UI.Text iconText = iconObj.GetComponent<UnityEngine.UI.Text>();
            iconText.fontStyle = FontStyle.Bold;
            iconText.raycastTarget = false;
            gameUI.perkButtonIconTexts[i] = iconText;
        }

        panel.SetActive(false);
    }

    GameObject CreateSlider(Transform parent, string name, Vector2 position, float min, float max, float defaultValue)
    {
        GameObject sliderObj = new GameObject(name);
        sliderObj.transform.SetParent(parent, false);

        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRect.anchoredPosition = position;
        sliderRect.sizeDelta = new Vector2(250, 20);

        UnityEngine.UI.Slider slider = sliderObj.AddComponent<UnityEngine.UI.Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = defaultValue;

        // Background
        GameObject sliderBg = new GameObject("Background");
        sliderBg.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRect = sliderBg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        UnityEngine.UI.Image bgImg = sliderBg.AddComponent<UnityEngine.UI.Image>();
        bgImg.color = new Color(0.25f, 0.25f, 0.25f, 1f);

        // Fill area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        UnityEngine.UI.Image fillImg = fill.AddComponent<UnityEngine.UI.Image>();
        fillImg.color = new Color(0.4f, 0.7f, 1f, 1f);
        slider.fillRect = fillRect;

        // Handle
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = Vector2.zero;
        handleAreaRect.offsetMax = Vector2.zero;

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRect = handle.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20, 20);
        UnityEngine.UI.Image handleImg = handle.AddComponent<UnityEngine.UI.Image>();
        handleImg.color = Color.white;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImg;

        return sliderObj;
    }

    void CreateMaterials()
    {
        EnsureDirectory("Assets/Materials");

        CreateDungeonMaterial("Assets/Materials/FloorMaterial.mat",
            new Color(0.15f, 0.13f, 0.12f), 0f, 0.1f);
        CreateDungeonMaterial("Assets/Materials/WallMaterial.mat",
            new Color(0.22f, 0.18f, 0.16f), 0f, 0.15f);
        CreateDungeonMaterial("Assets/Materials/CeilingMaterial.mat",
            new Color(0.1f, 0.09f, 0.08f), 0f, 0.05f);
        CreateColorMaterial("Assets/Materials/ExitMaterial.mat", new Color(0.1f, 0.9f, 0.3f));
        CreateColorMaterial("Assets/Materials/EnemyMaterial.mat", new Color(0.8f, 0.15f, 0.15f));
        CreateColorMaterial("Assets/Materials/ProjectileMaterial.mat", new Color(0.2f, 0.5f, 1f));
        CreateColorMaterial("Assets/Materials/SealMaterial.mat", new Color(0.7f, 0.1f, 0.1f));

        AssetDatabase.Refresh();
        Debug.Log("Materials created in Assets/Materials/");
    }

    void CreateDungeonMaterial(string path, Color color, float metallic, float smoothness)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.color = color;
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Smoothness", smoothness);

        AssetDatabase.CreateAsset(mat, path);
    }


    void CreateColorMaterial(string path, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material mat = new Material(shader);
        mat.color = color;

        if (mat.HasProperty("_EmissionColor"))
        {
            if (color.g > 0.5f && color.r < 0.3f)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 0.5f);
            }
        }

        AssetDatabase.CreateAsset(mat, path);
    }

    void CreateAllEnemyPrefabs()
    {
        EnsureDirectory("Assets/Prefabs");
        EnsureDirectory("Assets/Prefabs/Enemies");

        CreateEnemyPrefab("Grunt", EnemyType.Grunt, 30, 4f, 8, 2f,
            new Color(0.8f, 0.15f, 0.15f), new Vector3(0.9f, 0.9f, 0.9f));
        CreateEnemyPrefab("Ranger", EnemyType.Ranger, 20, 3f, 12, 8f,
            new Color(0.5f, 0.1f, 0.7f), new Vector3(0.9f, 0.9f, 0.9f));
        CreateEnemyPrefab("Tank", EnemyType.Tank, 80, 2f, 20, 2f,
            new Color(0.15f, 0.7f, 0.15f), new Vector3(1.08f, 1.08f, 1.08f));

        Debug.Log("All 3 enemy prefabs created in Assets/Prefabs/Enemies/");
    }

    void CreateEnemyPrefab(string typeName, EnemyType type, int hp, float speed,
        int damage, float attackRange, Color color, Vector3 scale)
    {
        string fbxName = "Skeleton";
        if (type == EnemyType.Ranger) fbxName = "Slime";
        if (type == EnemyType.Tank) fbxName = "Dragon";

        string fbxPath = "Assets/ExternalAssets/Monsters/FBX/" + fbxName + ".fbx";
        GameObject fbxModel = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);

        GameObject enemy;
        if (fbxModel != null)
        {
            enemy = (GameObject)PrefabUtility.InstantiatePrefab(fbxModel);
            enemy.name = typeName;
        }
        else
        {
            enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = typeName;
        }

        enemy.tag = "Enemy";
        enemy.layer = LayerMask.NameToLayer("Default");
        enemy.transform.localScale = scale;

        if (enemy.GetComponent<Collider>() == null)
        {
            CapsuleCollider capsule = enemy.AddComponent<CapsuleCollider>();
            capsule.height = 2f;
            capsule.radius = 0.4f;
            capsule.center = new Vector3(0, 1f, 0);
        }

        UnityEngine.AI.NavMeshAgent agent = enemy.AddComponent<UnityEngine.AI.NavMeshAgent>();
        agent.speed = speed;
        agent.stoppingDistance = (type == EnemyType.Ranger) ? 6f : 1.5f;
        agent.radius = type == EnemyType.Tank ? 0.68f : 0.42f;
        agent.height = type == EnemyType.Tank ? 1.75f : 1.8f;

        EnemyAI ai = enemy.AddComponent<EnemyAI>();
        ai.enemyType = type;
        ai.attackDamage = damage;
        ai.attackRange = attackRange;

        if (type == EnemyType.Ranger)
        {
            ai.preferredDistance = 8f;
            ai.attackCooldown = 2f;
            GameObject projPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Combat/Projectile.prefab");
            if (projPrefab != null)
            {
                ai.projectilePrefab = projPrefab;
            }
        }

        HealthSystem health = enemy.AddComponent<HealthSystem>();
        health.maxHealth = hp;

        EnemyDeathHandler deathHandler = enemy.AddComponent<EnemyDeathHandler>();
        GameObject explosionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VFX/Explosion.prefab");
        if (explosionPrefab != null)
        {
            deathHandler.deathVFXPrefab = explosionPrefab;
        }

        AudioClip deathSFX = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/ExternalAssets/KenneyImpact/Audio/impactPunch_heavy_002.ogg");
        if (deathSFX != null)
        {
            SerializedObject deathSO = new SerializedObject(deathHandler);
            SerializedProperty sfxProp = deathSO.FindProperty("deathSFX");
            if (sfxProp != null) sfxProp.objectReferenceValue = deathSFX;
            deathSO.ApplyModifiedProperties();
        }

        if (fbxModel == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material mat = new Material(shader);
            mat.color = color;
            string matPath = "Assets/Materials/" + typeName + "Material.mat";
            AssetDatabase.CreateAsset(mat, matPath);

            Renderer[] renderers = enemy.GetComponentsInChildren<Renderer>();
            for (int r = 0; r < renderers.Length; r++)
            {
                renderers[r].material = mat;
            }
        }

        if (enemy.GetComponent<Animator>() == null)
        {
            enemy.AddComponent<Animator>();
        }

        string prefabPath = "Assets/Prefabs/Enemies/" + typeName + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(enemy, prefabPath);
        DestroyImmediate(enemy);
    }

    void CreateEnemyAnimatorControllers()
    {
        EnsureDirectory("Assets/Animations");
        EnsureDirectory("Assets/Animations/Enemies");

        string[] enemyNames = { "Grunt", "Ranger", "Tank" };
        string[] fbxNames = { "Skeleton", "Slime", "Dragon" };

        for (int i = 0; i < enemyNames.Length; i++)
        {
            string controllerPath = "Assets/Animations/Enemies/" + enemyNames[i] + "Controller.controller";
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            controller.AddParameter("isMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("die", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine rootSM = controller.layers[0].stateMachine;

            string fbxPath = "Assets/ExternalAssets/Monsters/FBX/" + fbxNames[i] + ".fbx";
            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);

            AnimationClip idleClip = null;
            AnimationClip walkClip = null;
            AnimationClip attackClip = null;
            AnimationClip dieClip = null;

            for (int a = 0; a < allAssets.Length; a++)
            {
                AnimationClip clip = allAssets[a] as AnimationClip;
                if (clip == null) continue;
                if (clip.name.StartsWith("__")) continue;

                string clipNameLower = clip.name.ToLower();

                if (clipNameLower.Contains("idle") || clipNameLower.Contains("breathing"))
                    idleClip = clip;
                else if (clipNameLower.Contains("walk") || clipNameLower.Contains("run") || clipNameLower.Contains("crawl") || clipNameLower.Contains("flying") || clipNameLower.Contains("jump"))
                    walkClip = clip;
                else if (clipNameLower.Contains("attack") || clipNameLower.Contains("bite") || clipNameLower.Contains("punch"))
                    attackClip = clip;
                else if (clipNameLower.Contains("die") || clipNameLower.Contains("death"))
                    dieClip = clip;
            }

            AnimatorState idleState = rootSM.AddState("Idle");
            if (idleClip != null)
            {
                idleState.motion = idleClip;
            }
            rootSM.defaultState = idleState;

            AnimatorState walkState = rootSM.AddState("Walk");
            if (walkClip != null)
            {
                walkState.motion = walkClip;
            }

            AnimatorState attackState = rootSM.AddState("Attack");
            if (attackClip != null)
            {
                attackState.motion = attackClip;
            }

            AnimatorState dieState = rootSM.AddState("Die");
            if (dieClip != null)
            {
                dieState.motion = dieClip;
            }

            AnimatorStateTransition idleToWalk = idleState.AddTransition(walkState);
            idleToWalk.AddCondition(AnimatorConditionMode.If, 0, "isMoving");
            idleToWalk.hasExitTime = false;
            idleToWalk.duration = 0.15f;

            AnimatorStateTransition walkToIdle = walkState.AddTransition(idleState);
            walkToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isMoving");
            walkToIdle.hasExitTime = false;
            walkToIdle.duration = 0.15f;

            AnimatorStateTransition idleToAttack = idleState.AddTransition(attackState);
            idleToAttack.AddCondition(AnimatorConditionMode.If, 0, "attack");
            idleToAttack.hasExitTime = false;
            idleToAttack.duration = 0.1f;

            AnimatorStateTransition walkToAttack = walkState.AddTransition(attackState);
            walkToAttack.AddCondition(AnimatorConditionMode.If, 0, "attack");
            walkToAttack.hasExitTime = false;
            walkToAttack.duration = 0.1f;

            AnimatorStateTransition attackToIdle = attackState.AddTransition(idleState);
            attackToIdle.hasExitTime = true;
            attackToIdle.exitTime = 0.9f;
            attackToIdle.duration = 0.15f;

            AnimatorStateTransition anyToDie = rootSM.AddAnyStateTransition(dieState);
            anyToDie.AddCondition(AnimatorConditionMode.If, 0, "die");
            anyToDie.hasExitTime = false;
            anyToDie.duration = 0.1f;

            EditorUtility.SetDirty(controller);

            string prefabPath = "Assets/Prefabs/Enemies/" + enemyNames[i] + ".prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Animator animator = instance.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = instance.AddComponent<Animator>();
                }
                animator.runtimeAnimatorController = controller;
                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                DestroyImmediate(instance);
            }

            Debug.Log("Animator controller created for " + enemyNames[i] + " at " + controllerPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    GameObject CreateWeaponViewModelChild(Transform parent, string objectName, string assetPath,
        Vector3 localPosition, Vector3 localEuler, float localScale)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        GameObject weapon;
        if (source != null)
        {
            weapon = (GameObject)PrefabUtility.InstantiatePrefab(source);
            weapon.name = objectName;
        }
        else
        {
            weapon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            weapon.name = objectName;
        }

        weapon.transform.SetParent(parent, false);
        weapon.transform.localPosition = localPosition;
        weapon.transform.localRotation = Quaternion.Euler(localEuler);
        weapon.transform.localScale = Vector3.one * localScale;

        Collider[] colliders = weapon.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            DestroyImmediate(colliders[i]);
        }

        return weapon;
    }

    void ConfigureWeaponViewModel(WeaponViewModelController viewModel)
    {
        if (viewModel == null) return;

        viewModel.restLocalPosition = new Vector3(0.72f, -0.42f, 1.12f);
        viewModel.restLocalEuler = new Vector3(12f, -34f, 9f);
        viewModel.swingLocalOffset = new Vector3(-0.34f, 0.16f, 0.28f);
        viewModel.swingLocalEulerOffset = new Vector3(58f, 52f, -36f);
        viewModel.visibleScaleMultiplier = 1.34f;

        SetWeaponModelPose(viewModel.swordModel, Vector3.zero, new Vector3(74f, -18f, -12f), 0.96f);
        SetWeaponModelPose(viewModel.axeModel, Vector3.zero, new Vector3(72f, -20f, -15f), 1.02f);
        SetWeaponModelPose(viewModel.spearModel, new Vector3(0f, -0.08f, 0.16f), new Vector3(84f, -10f, -6f), 1.15f);
        SetWeaponModelPose(viewModel.hammerModel, Vector3.zero, new Vector3(72f, -22f, -18f), 1.04f);
        SetWeaponModelPose(viewModel.bowModel, new Vector3(-0.04f, -0.03f, 0.1f), new Vector3(66f, -14f, -88f), 1.08f);
    }

    void SetWeaponModelPose(GameObject model, Vector3 localPosition, Vector3 localEuler, float localScale)
    {
        if (model == null) return;

        model.transform.localPosition = localPosition;
        model.transform.localRotation = Quaternion.Euler(localEuler);
        model.transform.localScale = Vector3.one * localScale;
    }

    void CreateProjectilePrefab()
    {
        EnsureDirectory("Assets/Prefabs");
        EnsureDirectory("Assets/Prefabs/Combat");

        GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectile.name = "Projectile";
        projectile.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

        Collider col = projectile.GetComponent<Collider>();
        if (col is SphereCollider sphere)
        {
            sphere.isTrigger = true;
        }

        Rigidbody rb = projectile.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;

        Projectile proj = projectile.AddComponent<Projectile>();
        proj.speed = 20f;
        proj.damage = 25;
        proj.explosionRadius = 2.5f;

        GameObject lightObj = new GameObject("ProjectileLight");
        lightObj.transform.SetParent(projectile.transform);
        lightObj.transform.localPosition = Vector3.zero;
        Light pointLight = lightObj.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.range = 3f;
        pointLight.intensity = 2f;
        pointLight.color = new Color(0.3f, 0.6f, 1f);

        Material projMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ProjectileMaterial.mat");
        if (projMat != null)
        {
            projectile.GetComponent<Renderer>().material = projMat;
        }

        TrailRenderer trail = projectile.AddComponent<TrailRenderer>();
        trail.time = 0.3f;
        trail.startWidth = 0.15f;
        trail.endWidth = 0.0f;
        trail.startColor = new Color(0.3f, 0.6f, 1f, 0.8f);
        trail.endColor = new Color(0.3f, 0.6f, 1f, 0f);
        trail.material = CreateURPParticleMaterial(new Color(0.3f, 0.6f, 1f, 0.8f), "ProjectileTrail");

        HitEffectSpawner hitSpawner = projectile.AddComponent<HitEffectSpawner>();

        string prefabPath = "Assets/Prefabs/Combat/Projectile.prefab";
        PrefabUtility.SaveAsPrefabAsset(projectile, prefabPath);
        DestroyImmediate(projectile);

        Debug.Log("Projectile prefab created at " + prefabPath);
    }

    void CreateTorchPrefab()
    {
        EnsureDirectory("Assets/Prefabs");
        EnsureDirectory("Assets/Prefabs/Dungeon");

        GameObject torch = new GameObject("Torch");

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "TorchBody";
        body.transform.SetParent(torch.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(0.1f, 0.3f, 0.1f);
        DestroyImmediate(body.GetComponent<Collider>());

        // Point light
        GameObject lightObj = new GameObject("TorchLight");
        lightObj.transform.SetParent(torch.transform);
        lightObj.transform.localPosition = new Vector3(0, 0.4f, 0);
        Light pointLight = lightObj.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.range = 16f;
        pointLight.intensity = 3.5f;
        pointLight.color = new Color(1f, 0.8f, 0.45f);

        DungeonTorch flickerScript = torch.AddComponent<DungeonTorch>();
        flickerScript.torchLight = pointLight;

        // Fire particles
        GameObject fireObj = new GameObject("FireParticles");
        fireObj.transform.SetParent(torch.transform);
        fireObj.transform.localPosition = new Vector3(0, 0.35f, 0);
        ParticleSystem firePS = fireObj.AddComponent<ParticleSystem>();
        var fireMain = firePS.main;
        fireMain.startLifetime = 0.5f;
        fireMain.startSpeed = 0.5f;
        fireMain.startSize = 0.15f;
        fireMain.startColor = new Color(1f, 0.6f, 0.1f);
        fireMain.maxParticles = 20;
        fireMain.loop = true;
        fireMain.simulationSpace = ParticleSystemSimulationSpace.Local;
        var fireEmission = firePS.emission;
        fireEmission.rateOverTime = 15;
        var fireShape = firePS.shape;
        fireShape.shapeType = ParticleSystemShapeType.Cone;
        fireShape.angle = 15f;
        fireShape.radius = 0.02f;
        var fireColorOverLifetime = firePS.colorOverLifetime;
        fireColorOverLifetime.enabled = true;
        Gradient fireGrad = new Gradient();
        fireGrad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.8f, 0.2f), 0),
                new GradientColorKey(new Color(1f, 0.3f, 0f), 0.7f),
                new GradientColorKey(new Color(0.2f, 0.05f, 0f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1, 0),
                new GradientAlphaKey(0.6f, 0.7f),
                new GradientAlphaKey(0, 1)
            }
        );
        fireColorOverLifetime.color = new ParticleSystem.MinMaxGradient(fireGrad);
        var fireSizeOverLifetime = firePS.sizeOverLifetime;
        fireSizeOverLifetime.enabled = true;
        fireSizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0));

        var fireRenderer = firePS.GetComponent<ParticleSystemRenderer>();
        fireRenderer.material = CreateURPParticleMaterial(new Color(1f, 0.6f, 0.1f), "TorchFire");

        string prefabPath = "Assets/Prefabs/Dungeon/Torch.prefab";
        PrefabUtility.SaveAsPrefabAsset(torch, prefabPath);
        DestroyImmediate(torch);

        Debug.Log("Torch prefab created at " + prefabPath);
    }

    void CreateExplosionPrefab()
    {
        EnsureDirectory("Assets/Prefabs");
        EnsureDirectory("Assets/Prefabs/VFX");

        GameObject explosion = new GameObject("Explosion");

        // Particle system for explosion
        ParticleSystem ps = explosion.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.5f;
        main.startLifetime = 0.8f;
        main.startSpeed = 8f;
        main.startSize = 0.3f;
        main.startColor = new Color(1f, 0.5f, 0.1f);
        main.maxParticles = 50;
        main.loop = false;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0, 30)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.8f, 0.2f), 0),
                new GradientColorKey(new Color(1f, 0.2f, 0f), 0.5f),
                new GradientColorKey(new Color(0.3f, 0.1f, 0.1f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1, 0),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0, 1)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0));

        var psRenderer = ps.GetComponent<ParticleSystemRenderer>();
        psRenderer.material = CreateURPParticleMaterial(new Color(1f, 0.5f, 0.1f), "Explosion");

        // Light
        GameObject lightObj = new GameObject("ExplosionLight");
        lightObj.transform.SetParent(explosion.transform);
        lightObj.transform.localPosition = Vector3.zero;
        Light expLight = lightObj.AddComponent<Light>();
        expLight.type = LightType.Point;
        expLight.range = 10f;
        expLight.intensity = 5f;
        expLight.color = new Color(1f, 0.6f, 0.2f);

        ExplosionVFXTrigger trigger = explosion.AddComponent<ExplosionVFXTrigger>();
        trigger.explosionParticles = ps;
        trigger.impulseForce = 0f;

        CinemachineImpulseSource impulseSource = explosion.AddComponent<CinemachineImpulseSource>();

        string prefabPath = "Assets/Prefabs/VFX/Explosion.prefab";
        PrefabUtility.SaveAsPrefabAsset(explosion, prefabPath);
        DestroyImmediate(explosion);

        Debug.Log("Explosion prefab created at " + prefabPath);
    }

    Material CreateURPParticleMaterial(Color color, string assetName)
    {
        EnsureDirectory("Assets/Materials/Particles");
        string path = "Assets/Materials/Particles/" + assetName + ".mat";

        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Particles/Standard Unlit");
        }
        Material mat = new Material(shader);
        mat.color = color;
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 1f);
        mat.SetFloat("_AlphaClip", 0f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.EnableKeyword("_BLENDMODE_ADD");
        AssetDatabase.CreateAsset(mat, path);
        return mat;
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
