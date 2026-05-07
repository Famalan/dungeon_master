using System;
using System.Collections.Generic;
using UnityEngine;

public enum ArtifactRarity
{
    Common,
    Rare,
    Epic
}

public enum ArtifactTag
{
    Flame,
    Shadow,
    Gold,
    Vitality,
    Momentum
}

[Serializable]
public class ArtifactData
{
    public string id;
    public string displayName;
    public string description;
    public ArtifactRarity rarity;
    public ArtifactTag tag;
    public Color color;
    public string iconGlyph;

    readonly Action<PlayerStats> apply;

    public ArtifactData(
        string id,
        string displayName,
        string description,
        ArtifactRarity rarity,
        ArtifactTag tag,
        Color color,
        Action<PlayerStats> apply)
        : this(id, displayName, description, rarity, tag, color, GetDefaultIcon(tag), apply)
    {
    }

    public ArtifactData(
        string id,
        string displayName,
        string description,
        ArtifactRarity rarity,
        ArtifactTag tag,
        Color color,
        string iconGlyph,
        Action<PlayerStats> apply)
    {
        this.id = id;
        this.displayName = displayName;
        this.description = description;
        this.rarity = rarity;
        this.tag = tag;
        this.color = color;
        this.iconGlyph = string.IsNullOrEmpty(iconGlyph) ? GetDefaultIcon(tag) : iconGlyph;
        this.apply = apply;
    }

    public void Apply(PlayerStats stats)
    {
        if (stats != null && apply != null)
        {
            apply(stats);
        }
    }

    static string GetDefaultIcon(ArtifactTag tag)
    {
        switch (tag)
        {
            case ArtifactTag.Flame: return "F";
            case ArtifactTag.Shadow: return "S";
            case ArtifactTag.Gold: return "G";
            case ArtifactTag.Vitality: return "+";
            case ArtifactTag.Momentum: return ">";
            default: return "*";
        }
    }
}

[Serializable]
public class ArtifactInventory
{
    readonly List<string> artifactIds = new List<string>();
    readonly List<string> artifactNames = new List<string>();
    readonly Dictionary<ArtifactTag, int> tagCounts = new Dictionary<ArtifactTag, int>();
    readonly HashSet<string> activeSynergies = new HashSet<string>();

    public int Count { get { return artifactIds.Count; } }

    public bool Contains(string id)
    {
        return artifactIds.Contains(id);
    }

    public bool Add(ArtifactData artifact)
    {
        if (artifact == null || string.IsNullOrEmpty(artifact.id)) return false;
        if (Contains(artifact.id)) return false;

        artifactIds.Add(artifact.id);
        artifactNames.Add(artifact.displayName);

        int count;
        tagCounts.TryGetValue(artifact.tag, out count);
        tagCounts[artifact.tag] = count + 1;
        return true;
    }

    public int GetTagCount(ArtifactTag tag)
    {
        int count;
        return tagCounts.TryGetValue(tag, out count) ? count : 0;
    }

    public bool ActivateSynergy(string key)
    {
        if (activeSynergies.Contains(key)) return false;
        activeSynergies.Add(key);
        return true;
    }

    public void Clear()
    {
        artifactIds.Clear();
        artifactNames.Clear();
        tagCounts.Clear();
        activeSynergies.Clear();
    }

    public string GetSummary()
    {
        if (artifactNames.Count == 0) return "No artifacts";
        return string.Join(", ", artifactNames.ToArray());
    }
}

public class ArtifactSystem : MonoBehaviour
{
    ArtifactData[] allArtifacts;

    void Awake()
    {
        EnsureInitialized();
    }

    public ArtifactData[] GetAllArtifacts()
    {
        EnsureInitialized();
        return allArtifacts;
    }

    public ArtifactData[] GetChoices(int count)
    {
        EnsureInitialized();

        int resultCount = Mathf.Clamp(count, 0, allArtifacts.Length);
        ArtifactData[] result = new ArtifactData[resultCount];
        bool[] used = new bool[allArtifacts.Length];

        for (int i = 0; i < result.Length; i++)
        {
            int attempts = 0;
            while (attempts < 100)
            {
                int index = UnityEngine.Random.Range(0, allArtifacts.Length);
                if (!used[index])
                {
                    used[index] = true;
                    result[i] = allArtifacts[index];
                    break;
                }
                attempts++;
            }

            if (result[i] == null)
            {
                for (int fallback = 0; fallback < allArtifacts.Length; fallback++)
                {
                    if (!used[fallback])
                    {
                        used[fallback] = true;
                        result[i] = allArtifacts[fallback];
                        break;
                    }
                }
            }
        }

        return result;
    }

    public bool ApplyArtifact(ArtifactData artifact, PlayerStats stats)
    {
        if (artifact == null || stats == null) return false;

        stats.EnsureArtifactInventory();
        if (stats.artifactInventory.Contains(artifact.id)) return false;

        artifact.Apply(stats);
        if (!stats.artifactInventory.Add(artifact)) return false;

        ApplySynergies(stats);
        return true;
    }

    void ApplySynergies(PlayerStats stats)
    {
        ApplyTagSynergy(stats, ArtifactTag.Flame, 2, "flame_2", () => stats.damageMultiplier += 0.15f);
        ApplyTagSynergy(stats, ArtifactTag.Flame, 4, "flame_4", () => stats.blastRadiusMultiplier += 0.25f);

        ApplyTagSynergy(stats, ArtifactTag.Shadow, 2, "shadow_2", () => stats.dashCooldownMultiplier *= 0.85f);
        ApplyTagSynergy(stats, ArtifactTag.Shadow, 4, "shadow_4", () => stats.dashIFrameBonus += 0.15f);

        ApplyTagSynergy(stats, ArtifactTag.Gold, 2, "gold_2", () => stats.lootChanceBonus += 0.25f);
        ApplyTagSynergy(stats, ArtifactTag.Gold, 4, "gold_4", () =>
        {
            stats.chestCoinBonus += 3;
            stats.artifactRerollDiscount += 2;
        });

        ApplyTagSynergy(stats, ArtifactTag.Vitality, 2, "vitality_2", () => stats.maxHealthBonus += 30);
        ApplyTagSynergy(stats, ArtifactTag.Vitality, 4, "vitality_4", () => stats.levelStartHeal += 15);

        ApplyTagSynergy(stats, ArtifactTag.Momentum, 2, "momentum_2", () => stats.speedMultiplier += 0.10f);
        ApplyTagSynergy(stats, ArtifactTag.Momentum, 4, "momentum_4", () => stats.fireRateMultiplier += 0.25f);
    }

    void ApplyTagSynergy(PlayerStats stats, ArtifactTag tag, int threshold, string key, Action apply)
    {
        if (stats.artifactInventory.GetTagCount(tag) < threshold) return;
        if (!stats.artifactInventory.ActivateSynergy(key)) return;
        apply();
    }

    void EnsureInitialized()
    {
        if (allArtifacts != null && allArtifacts.Length > 0) return;

        Color common = new Color(0.78f, 0.82f, 0.88f, 1f);
        Color rare = new Color(0.35f, 0.75f, 1f, 1f);
        Color epic = new Color(0.82f, 0.45f, 1f, 1f);

        allArtifacts = new ArtifactData[]
        {
            New("ember_core", "Ember Core", "Flame set: stronger hits", ArtifactRarity.Common, ArtifactTag.Flame, common, s => s.damageMultiplier += 0.12f),
            New("cinder_lens", "Cinder Lens", "Flame set: wider blasts", ArtifactRarity.Rare, ArtifactTag.Flame, rare, s => s.blastRadiusMultiplier += 0.10f),
            New("ash_catalyst", "Ash Catalyst", "Flame set: faster casting", ArtifactRarity.Common, ArtifactTag.Flame, common, s => s.fireRateMultiplier += 0.10f),
            New("volcanic_charm", "Volcanic Charm", "Flame set: burning power", ArtifactRarity.Epic, ArtifactTag.Flame, epic, s => s.damageMultiplier += 0.08f),

            New("nightstep_boots", "Nightstep Boots", "Shadow set: quicker dash", ArtifactRarity.Common, ArtifactTag.Shadow, common, s => s.dashCooldownMultiplier *= 0.92f),
            New("umbral_cloak", "Umbral Cloak", "Shadow set: safer dash", ArtifactRarity.Rare, ArtifactTag.Shadow, rare, s => s.dashIFrameBonus += 0.08f),
            New("echo_mask", "Echo Mask", "Shadow set: lighter movement", ArtifactRarity.Common, ArtifactTag.Shadow, common, s => s.speedMultiplier += 0.05f),
            New("void_splinter", "Void Splinter", "Shadow set: precise strikes", ArtifactRarity.Epic, ArtifactTag.Shadow, epic, s => s.damageMultiplier += 0.06f),

            New("coin_talisman", "Coin Talisman", "Gold set: richer drops", ArtifactRarity.Common, ArtifactTag.Gold, common, s => s.lootChanceBonus += 0.18f),
            New("gilded_key", "Gilded Key", "Gold set: fuller chests", ArtifactRarity.Rare, ArtifactTag.Gold, rare, s => s.chestCoinBonus += 2),
            New("merchant_seal", "Merchant Seal", "Gold set: cheaper rerolls", ArtifactRarity.Rare, ArtifactTag.Gold, rare, s => s.artifactRerollDiscount += 2),
            New("lucky_scale", "Lucky Scale", "Gold set: instant coins", ArtifactRarity.Common, ArtifactTag.Gold, common, s => s.AddCoins(3)),

            New("heartstone", "Heartstone", "Vitality set: deeper life pool", ArtifactRarity.Common, ArtifactTag.Vitality, common, s => s.maxHealthBonus += 15),
            New("moss_band", "Moss Band", "Vitality set: level recovery", ArtifactRarity.Common, ArtifactTag.Vitality, common, s => s.regenPerLevel += 5),
            New("blood_orchid", "Blood Orchid", "Vitality set: opening heal", ArtifactRarity.Rare, ArtifactTag.Vitality, rare, s => s.levelStartHeal += 8),
            New("guardian_totem", "Guardian Totem", "Vitality set: steadier survival", ArtifactRarity.Epic, ArtifactTag.Vitality, epic, s => s.maxHealthBonus += 10),

            New("quickspring", "Quickspring", "Momentum set: quicker shots", ArtifactRarity.Common, ArtifactTag.Momentum, common, s => s.fireRateMultiplier += 0.10f),
            New("wind_spur", "Wind Spur", "Momentum set: faster footwork", ArtifactRarity.Common, ArtifactTag.Momentum, common, s => s.speedMultiplier += 0.08f),
            New("kinetic_core", "Kinetic Core", "Momentum set: shorter dash wait", ArtifactRarity.Rare, ArtifactTag.Momentum, rare, s => s.dashCooldownMultiplier *= 0.95f),
            New("focus_gear", "Focus Gear", "Momentum set: cleaner hits", ArtifactRarity.Epic, ArtifactTag.Momentum, epic, s => s.damageMultiplier += 0.07f)
        };
    }

    ArtifactData New(
        string id,
        string displayName,
        string description,
        ArtifactRarity rarity,
        ArtifactTag tag,
        Color color,
        Action<PlayerStats> apply)
    {
        return new ArtifactData(id, displayName, description, rarity, tag, color, apply);
    }
}
