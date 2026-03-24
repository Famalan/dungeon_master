using System;

public static class GameEvents
{
    public static event Action<GameState> OnGameStateChanged;
    public static event Action<int> OnLevelStarted;
    public static event Action OnLevelGenerated;
    public static event Action OnPlayerDeath;
    public static event Action<int> OnCoinsChanged;
    public static event Action<string> OnSealNotification;
    public static event Action OnEnemyKilled;

    public static event Action OnRequestStartGame;
    public static event Action OnRequestRestartGame;
    public static event Action OnRequestQuitGame;
    public static event Action OnRequestPerkChosen;
    public static event Action OnWeaponModeChanged;
    public static event Action OnPlayerReachedExit;

    public static void FireGameStateChanged(GameState newState)
    {
        OnGameStateChanged?.Invoke(newState);
    }

    public static void FireLevelStarted(int level)
    {
        OnLevelStarted?.Invoke(level);
    }

    public static void FireLevelGenerated()
    {
        OnLevelGenerated?.Invoke();
    }

    public static void FirePlayerDeath()
    {
        OnPlayerDeath?.Invoke();
    }

    public static void FireCoinsChanged(int newAmount)
    {
        OnCoinsChanged?.Invoke(newAmount);
    }

    public static void FireSealNotification(string message)
    {
        OnSealNotification?.Invoke(message);
    }

    public static void FireEnemyKilled()
    {
        OnEnemyKilled?.Invoke();
    }

    public static void FireRequestStartGame()
    {
        OnRequestStartGame?.Invoke();
    }

    public static void FireRequestRestartGame()
    {
        OnRequestRestartGame?.Invoke();
    }

    public static void FireRequestQuitGame()
    {
        OnRequestQuitGame?.Invoke();
    }

    public static void FireRequestPerkChosen()
    {
        OnRequestPerkChosen?.Invoke();
    }

    public static void FireWeaponModeChanged()
    {
        OnWeaponModeChanged?.Invoke();
    }

    public static void FirePlayerReachedExit()
    {
        OnPlayerReachedExit?.Invoke();
    }
}
