using UnityEngine;

public enum GameState
{
    MainMenu,
    Playing,
    PerkSelection,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    public LevelManager levelManager;
    public PlayerController player;
    public HealthSystem playerHealth;

    GameState currentState;

    public GameState CurrentState { get { return currentState; } }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnEnable()
    {
        GameEvents.OnRequestStartGame += HandleStartGame;
        GameEvents.OnRequestRestartGame += HandleStartGame;
        GameEvents.OnRequestQuitGame += HandleQuit;
        GameEvents.OnPlayerDeath += HandlePlayerDeath;
    }

    void OnDisable()
    {
        GameEvents.OnRequestStartGame -= HandleStartGame;
        GameEvents.OnRequestRestartGame -= HandleStartGame;
        GameEvents.OnRequestQuitGame -= HandleQuit;
        GameEvents.OnPlayerDeath -= HandlePlayerDeath;
    }

    void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.onDeath.RemoveListener(OnPlayerDeathFromHealth);
        }
    }

    void Start()
    {
        if (playerHealth != null)
        {
            playerHealth.onDeath.AddListener(OnPlayerDeathFromHealth);
        }

        SetState(GameState.MainMenu);
    }

    public void SetState(GameState newState)
    {
        currentState = newState;
        GameEvents.FireGameStateChanged(newState);
    }

    void HandleStartGame()
    {
        if (levelManager != null)
        {
            levelManager.StartNewGame();
        }
    }

    void HandlePlayerDeath()
    {
        SetState(GameState.GameOver);
    }

    void OnPlayerDeathFromHealth()
    {
        GameEvents.FirePlayerDeath();
    }

    void HandleQuit()
    {
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
