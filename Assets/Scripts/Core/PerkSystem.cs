using UnityEngine;

public class PerkData
{
    public string Name;
    public string Description;
    public System.Action<PlayerStats> Apply;
}

public class PerkSystem : MonoBehaviour
{
    PerkData[] allPerks;

    void Awake()
    {
        allPerks = new PerkData[]
        {
            new PerkData
            {
                Name = "Thick Skin",
                Description = "Vitality synergy: deeper life pool",
                Apply = (stats) => { stats.maxHealthBonus += 20; }
            },
            new PerkData
            {
                Name = "Quick Hands",
                Description = "Momentum synergy: quicker shots",
                Apply = (stats) => { stats.fireRateMultiplier += 0.25f; }
            },
            new PerkData
            {
                Name = "Power Shot",
                Description = "Flame synergy: stronger hits",
                Apply = (stats) => { stats.damageMultiplier += 0.3f; }
            },
            new PerkData
            {
                Name = "Swift Feet",
                Description = "Momentum synergy: faster footwork",
                Apply = (stats) => { stats.speedMultiplier += 0.15f; }
            },
            new PerkData
            {
                Name = "Scavenger",
                Description = "Gold synergy: richer drops",
                Apply = (stats) => { stats.lootChanceBonus += 0.25f; }
            },
            new PerkData
            {
                Name = "Regeneration",
                Description = "Vitality synergy: level recovery",
                Apply = (stats) => { stats.regenPerLevel += 10; }
            }
        };
    }

    public PerkData[] GetRandomPerks(int count)
    {
        PerkData[] result = new PerkData[count];
        bool[] used = new bool[allPerks.Length];

        for (int i = 0; i < count; i++)
        {
            int attempts = 0;
            while (attempts < 50)
            {
                int idx = Random.Range(0, allPerks.Length);
                if (!used[idx])
                {
                    used[idx] = true;
                    result[i] = allPerks[idx];
                    break;
                }
                attempts++;
            }

            if (result[i] == null)
            {
                result[i] = allPerks[i % allPerks.Length];
            }
        }

        return result;
    }

    public void ApplyPerk(PerkData perk, PlayerStats stats)
    {
        if (perk != null && stats != null)
        {
            perk.Apply(stats);
        }
    }
}
