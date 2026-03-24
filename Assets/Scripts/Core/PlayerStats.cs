using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public int coins;
    public float damageMultiplier = 1f;
    public float speedMultiplier = 1f;
    public int maxHealthBonus;
    public float fireRateMultiplier = 1f;
    public float lootChanceBonus;
    public int regenPerLevel;

    public void ResetStats()
    {
        coins = 0;
        damageMultiplier = 1f;
        speedMultiplier = 1f;
        maxHealthBonus = 0;
        fireRateMultiplier = 1f;
        lootChanceBonus = 0f;
        regenPerLevel = 0;
    }

    public void AddCoins(int amount)
    {
        coins += amount;
        GameEvents.FireCoinsChanged(coins);
    }

    public bool TrySpendCoins(int amount)
    {
        if (amount <= 0) return true;
        if (coins < amount) return false;

        coins -= amount;
        GameEvents.FireCoinsChanged(coins);
        return true;
    }
}
