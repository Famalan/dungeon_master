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
    public float dashCooldownMultiplier = 1f;
    public float blastRadiusMultiplier = 1f;
    public float dashIFrameBonus;
    public int chestCoinBonus;
    public int levelStartHeal;
    public int artifactRerollDiscount;
    public ArtifactInventory artifactInventory = new ArtifactInventory();

    public void ResetStats()
    {
        coins = 0;
        damageMultiplier = 1f;
        speedMultiplier = 1f;
        maxHealthBonus = 0;
        fireRateMultiplier = 1f;
        lootChanceBonus = 0f;
        regenPerLevel = 0;
        dashCooldownMultiplier = 1f;
        blastRadiusMultiplier = 1f;
        dashIFrameBonus = 0f;
        chestCoinBonus = 0;
        levelStartHeal = 0;
        artifactRerollDiscount = 0;
        EnsureArtifactInventory();
        artifactInventory.Clear();
    }

    public void EnsureArtifactInventory()
    {
        if (artifactInventory == null)
        {
            artifactInventory = new ArtifactInventory();
        }
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
