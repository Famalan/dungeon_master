using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [Header("Dungeon")]
    public DungeonGenerator dungeonGenerator;
    public DungeonBuilder dungeonBuilder;
    public EnemySpawner enemySpawner;
    public RoomSealManager roomSealManager;

    [Header("Player")]
    public PlayerController player;
    public HealthSystem playerHealth;
    public PlayerStats playerStats;

    [Header("UI Extras")]
    public MinimapUI minimap;

    int currentLevel;

    void OnEnable()
    {
        GameEvents.OnGameStateChanged += HandleStateChanged;
        GameEvents.OnRequestPerkChosen += HandlePerkChosen;
        GameEvents.OnPlayerReachedExit += HandlePlayerReachedExit;
    }

    void OnDisable()
    {
        GameEvents.OnGameStateChanged -= HandleStateChanged;
        GameEvents.OnRequestPerkChosen -= HandlePerkChosen;
        GameEvents.OnPlayerReachedExit -= HandlePlayerReachedExit;
    }

    void HandlePlayerReachedExit()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        if (gm.CurrentState != GameState.Playing) return;

        NextLevel();
    }

    void HandleStateChanged(GameState newState)
    {
        if (newState == GameState.Playing)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (player != null)
            {
                player.SetActive(true);
                player.mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 2f);
            }
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (player != null)
            {
                player.SetActive(false);
            }
        }
    }

    public void StartNewGame()
    {
        currentLevel = 1;

        if (playerStats != null)
        {
            playerStats.ResetStats();
        }

        GenerateAndStartLevel();
    }

    void HandlePerkChosen()
    {
        GenerateAndStartLevel();
    }

    void NextLevel()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        currentLevel++;
        gm.SetState(GameState.PerkSelection);
    }

    public void GenerateAndStartLevel()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        ApplyPlayerStats();

        dungeonGenerator.Generate();
        dungeonBuilder.BuildDungeon();

        if (roomSealManager != null)
        {
            GameObject dungeon = GameObject.Find("Dungeon");
            Transform dungeonParent = (dungeon != null) ? dungeon.transform : null;
            roomSealManager.SetupRoomSealing(dungeonGenerator, dungeonBuilder, dungeonParent);
        }

        if (player != null)
        {
            player.TeleportTo(dungeonBuilder.PlayerSpawnPosition);
        }

        if (playerHealth != null)
        {
            if (currentLevel == 1)
            {
                playerHealth.ResetHealth();
            }
            else if (playerStats != null)
            {
                playerHealth.Heal(playerStats.regenPerLevel + playerStats.levelStartHeal);
            }
        }

        if (enemySpawner != null)
        {
            enemySpawner.SpawnEnemies(
                dungeonBuilder.EnemySpawnPoints,
                dungeonBuilder.EnemySpawnRoomDistances,
                currentLevel
            );
            enemySpawner.SpawnExitGuardian(dungeonBuilder.ExitPosition, currentLevel);
        }

        if (minimap != null)
        {
            minimap.dungeonGenerator = dungeonGenerator;
            minimap.playerTransform = player.transform;
            minimap.worldTileSize = dungeonBuilder.tileSize;
            minimap.GenerateMapTexture();
        }

        if (currentLevel == 1 && playerStats != null && playerStats.regenPerLevel > 0 && playerHealth != null)
        {
            playerHealth.Heal(playerStats.regenPerLevel);
        }

        GameEvents.FireLevelStarted(currentLevel);
        GameEvents.FireLevelGenerated();

        // После телепорта на спавн — иначе при смене состояния игрок ещё стоит у старого выхода.
        gm.SetState(GameState.Playing);
    }

    void ApplyPlayerStats()
    {
        if (playerStats == null) return;

        if (playerHealth != null)
        {
            playerHealth.maxHealth = 100 + playerStats.maxHealthBonus;
        }

        if (player != null)
        {
            player.moveSpeed = 6f * playerStats.speedMultiplier;
            player.dashCooldown = 1.5f * playerStats.dashCooldownMultiplier;
            player.dashIFrames = 0.2f + playerStats.dashIFrameBonus;

            ProjectileShooter shooter = player.shooter;
            if (shooter != null)
            {
                shooter.fireCooldown = 0.3f * (1f / playerStats.fireRateMultiplier);
            }

            if (player.meleeWeapon != null)
            {
                player.meleeWeapon.damageMultiplierSource = playerStats;
            }
        }
    }

    public int CurrentLevel { get { return currentLevel; } }
}
