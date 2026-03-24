using UnityEngine;
using UnityEngine.UI;

public class KillStreakUI : MonoBehaviour
{
    public Text streakText;
    public AudioClip streakBonusClip;

    int streak;
    float lastKillTime;
    float hideTimer;

    void OnEnable()
    {
        GameEvents.OnEnemyKilled += OnEnemyKilled;
        GameEvents.OnGameStateChanged += OnGameState;
    }

    void OnDisable()
    {
        GameEvents.OnEnemyKilled -= OnEnemyKilled;
        GameEvents.OnGameStateChanged -= OnGameState;
    }

    void OnGameState(GameState state)
    {
        if (state != GameState.Playing)
        {
            streak = 0;
            if (streakText != null)
            {
                streakText.gameObject.SetActive(false);
            }
        }
    }

    void OnEnemyKilled()
    {
        if (Time.time - lastKillTime > 2.35f)
        {
            streak = 0;
        }

        streak++;
        lastKillTime = Time.time;

        if (streak >= 5 && streak % 5 == 0)
        {
            PlayerStats stats = Object.FindAnyObjectByType<PlayerStats>();
            if (stats != null)
            {
                int bonus = 2;
                if (streak >= 10) bonus++;
                if (streak >= 15) bonus++;
                stats.AddCoins(bonus);
            }

            if (streakBonusClip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(streakBonusClip, 0.8f);
            }
        }

        if (streakText == null) return;

        if (streak >= 2)
        {
            streakText.gameObject.SetActive(true);
            streakText.text = "STREAK x" + streak.ToString();
            hideTimer = 2f;
        }
    }

    void Update()
    {
        if (streakText == null) return;

        if (hideTimer > 0f)
        {
            hideTimer -= Time.deltaTime;
            if (streakText.gameObject.activeInHierarchy)
            {
                float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * 12f);
                Color c = streakText.color;
                c.a = pulse;
                streakText.color = c;
            }
        }

        if (hideTimer <= 0f && streakText.gameObject.activeInHierarchy)
        {
            streakText.gameObject.SetActive(false);
        }
    }
}
