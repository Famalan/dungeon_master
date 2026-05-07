using UnityEngine;
using UnityEngine.Events;

public class HealthSystem : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Events")]
    public UnityEvent onDeath;
    public UnityEvent<int, int> onHealthChanged; // currentHP, maxHP
    public UnityEvent onDamageTaken;

    [Header("Audio")]
    public bool playDamageSFX = true;

    bool isDead;
    PlayerController playerController;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    void Start()
    {
        currentHealth = maxHealth;
        isDead = false;
        onHealthChanged.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        if (playerController != null && playerController.IsInvulnerable) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);
        onHealthChanged.Invoke(currentHealth, maxHealth);
        onDamageTaken.Invoke();

        if (playDamageSFX && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDamageTaken();
        }

        if (CombatTextManager.Instance != null)
        {
            bool isPlayer = CompareTag("Player");
            CombatTextManager.Instance.ShowDamage(transform.position, damage, isPlayer);
        }

        if (currentHealth <= 0)
        {
            isDead = true;
            onDeath.Invoke();
        }
    }

    public void Heal(int amount)
    {
        if (isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        onHealthChanged.Invoke(currentHealth, maxHealth);
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
        onHealthChanged.Invoke(currentHealth, maxHealth);
    }

    public float GetHealthPercent()
    {
        return (float)currentHealth / maxHealth;
    }
}
