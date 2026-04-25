using System.Collections.ObjectModel;

namespace SkyblockRepo.Models;

/// <summary>
/// Static reference data for SkyBlock fishing metadata.
/// </summary>
public class SkyblockFishingData
{
	/// <summary>
	/// Gets water sea creature ids.
	/// </summary>
	public ReadOnlyCollection<string> WaterCreatureIds { get; internal set; } = StaticMetadataDefaults.EmptyList<string>();

	/// <summary>
	/// Gets lava sea creature ids.
	/// </summary>
	public ReadOnlyCollection<string> LavaCreatureIds { get; internal set; } = StaticMetadataDefaults.EmptyList<string>();

	/// <summary>
	/// Gets trophy fish keyed by trophy fish id.
	/// </summary>
	public ReadOnlyDictionary<string, SkyblockTrophyFishDefinition> TrophyFishById { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<SkyblockTrophyFishDefinition>();

	/// <summary>
	/// Gets ordered trophy fish tier ids.
	/// </summary>
	public ReadOnlyCollection<string> TrophyFishTiers { get; internal set; } = StaticMetadataDefaults.EmptyList<string>();

	/// <summary>
	/// Gets ordered trophy fish rank stage names.
	/// </summary>
	public ReadOnlyCollection<string> TrophyFishStages { get; internal set; } = StaticMetadataDefaults.EmptyList<string>();

	/// <summary>
	/// Gets dolphin milestone brackets.
	/// </summary>
	public ReadOnlyCollection<SkyblockMilestoneBracket> DolphinBrackets { get; internal set; } = StaticMetadataDefaults.EmptyList<SkyblockMilestoneBracket>();

	/// <summary>
	/// Gets a trophy fish definition by id.
	/// </summary>
	public SkyblockTrophyFishDefinition? GetTrophyFish(string trophyFishId) => TrophyFishById.GetValueOrDefault(trophyFishId);
}

/// <summary>
/// A trophy fish definition.
/// </summary>
public class SkyblockTrophyFishDefinition
{
	public string TrophyFishId { get; init; } = string.Empty;
	public string DisplayName { get; init; } = string.Empty;
	public string Description { get; init; } = string.Empty;
	public ReadOnlyDictionary<string, int> ThresholdsByTier { get; init; } = StaticMetadataDefaults.EmptyDictionary<int>();
	public ReadOnlyDictionary<string, SkyblockDisplayIcon> IconsByTier { get; init; } = StaticMetadataDefaults.EmptyDictionary<SkyblockDisplayIcon>();
}

/// <summary>
/// A static milestone bracket.
/// </summary>
public class SkyblockMilestoneBracket
{
	public string Name { get; init; } = string.Empty;
	public string Rarity { get; init; } = string.Empty;
	public int Requirement { get; init; }
}
