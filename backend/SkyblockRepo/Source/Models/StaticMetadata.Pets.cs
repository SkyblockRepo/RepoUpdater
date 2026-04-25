using System.Collections.ObjectModel;

namespace SkyblockRepo.Models;

/// <summary>
/// Static reference data for pet progression and pet-score metadata.
/// </summary>
public class SkyblockPetCatalogData
{
	/// <summary>
	/// Gets pet catalog entries keyed by pet id.
	/// </summary>
	public ReadOnlyDictionary<string, SkyblockPetCatalogEntry> ByPetId { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<SkyblockPetCatalogEntry>();

	/// <summary>
	/// Gets default pet level experience.
	/// </summary>
	public ReadOnlyCollection<int> LevelExperience { get; internal set; } = StaticMetadataDefaults.EmptyList<int>();

	/// <summary>
	/// Gets rarity offset values keyed by rarity.
	/// </summary>
	public ReadOnlyDictionary<string, int> RarityOffsets { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<int>();

	/// <summary>
	/// Gets custom pet leveling tables keyed by pet id.
	/// </summary>
	public ReadOnlyDictionary<string, SkyblockPetLevelingDefinition> CustomLeveling { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<SkyblockPetLevelingDefinition>();

	/// <summary>
	/// Gets pet score rewards keyed by required score.
	/// </summary>
	public ReadOnlyDictionary<int, SkyblockPetScoreReward> ScoreRewards { get; internal set; } = StaticMetadataDefaults.EmptyIntDictionary<SkyblockPetScoreReward>();

	/// <summary>
	/// Gets pet aliases keyed by pet id with the parent or family id as value.
	/// </summary>
	public ReadOnlyDictionary<string, string> ParentAliases { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<string>();

	/// <summary>
	/// Gets held-item display-name mappings keyed by display name.
	/// </summary>
	public ReadOnlyDictionary<string, string> HeldItemDisplayNameToId { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<string>();
}

/// <summary>
/// A pet catalog entry.
/// </summary>
public class SkyblockPetCatalogEntry
{
	public string PetId { get; init; } = string.Empty;
	public string DisplayName { get; init; } = string.Empty;
	public string? SkillType { get; init; }
	public string? ParentId { get; init; }
	public ReadOnlyCollection<string> AvailableRarities { get; init; } = StaticMetadataDefaults.EmptyList<string>();
	public string? MaxRarity { get; init; }
	public ReadOnlyDictionary<string, SkyblockPetCatalogRarityDefinition> Rarities { get; init; } = StaticMetadataDefaults.EmptyDictionary<SkyblockPetCatalogRarityDefinition>();
	public SkyblockDisplayIcon Icon { get; init; } = new();
}

/// <summary>
/// Static pet data for a specific rarity.
/// </summary>
public class SkyblockPetCatalogRarityDefinition
{
	public string Rarity { get; init; } = string.Empty;
	public ReadOnlyDictionary<string, double> LevelOneStats { get; init; } = new(new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase));
	public ReadOnlyDictionary<string, double> LevelHundredStats { get; init; } = new(new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase));
	public ReadOnlyCollection<double> LevelOneOtherNumbers { get; init; } = StaticMetadataDefaults.EmptyList<double>();
	public ReadOnlyCollection<double> LevelHundredOtherNumbers { get; init; } = StaticMetadataDefaults.EmptyList<double>();
}

/// <summary>
/// A custom pet leveling definition.
/// </summary>
public class SkyblockPetLevelingDefinition
{
	public string PetId { get; init; } = string.Empty;
	public int Type { get; init; }
	public int MaxLevel { get; init; }
	public ReadOnlyCollection<int> LevelExperience { get; init; } = StaticMetadataDefaults.EmptyList<int>();
	public double? ExperienceMultiplier { get; init; }
}

/// <summary>
/// A pet score reward threshold.
/// </summary>
public class SkyblockPetScoreReward
{
	public int RequiredScore { get; init; }
	public ReadOnlyDictionary<string, double> Bonuses { get; init; } = new(new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase));
}
