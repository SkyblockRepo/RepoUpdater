using System.Collections.ObjectModel;

namespace SkyblockRepo.Models;

/// <summary>
/// Static reference data for collection categories and thresholds.
/// The collection thresholds come from the cached Hypixel collections resource.
/// </summary>
public class SkyblockCollectionsData
{
	/// <summary>
	/// Gets the upstream Hypixel resource version when available.
	/// </summary>
	public string Version { get; internal set; } = string.Empty;

	/// <summary>
	/// Gets the upstream Hypixel resource update time when available.
	/// </summary>
	public DateTimeOffset? LastUpdated { get; internal set; }

	/// <summary>
	/// Gets collection categories keyed by category id.
	/// </summary>
	public ReadOnlyDictionary<string, SkyblockCollectionCategory> ByCategoryId { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<SkyblockCollectionCategory>();

	/// <summary>
	/// Gets collection entries keyed by item id.
	/// </summary>
	public ReadOnlyDictionary<string, SkyblockCollectionEntry> ByItemId { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<SkyblockCollectionEntry>();

	/// <summary>
	/// Gets boss collections keyed by boss id.
	/// </summary>
	public ReadOnlyDictionary<string, SkyblockBossCollection> BossCollections { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<SkyblockBossCollection>();

	/// <summary>
	/// Gets a collection entry by item id.
	/// </summary>
	public SkyblockCollectionEntry? GetEntry(string itemId) => ByItemId.GetValueOrDefault(itemId);
}

/// <summary>
/// A collection category definition.
/// </summary>
public class SkyblockCollectionCategory
{
	public string CategoryId { get; init; } = string.Empty;
	public string Name { get; init; } = string.Empty;
	public SkyblockDisplayIcon Icon { get; init; } = new();
	public ReadOnlyCollection<SkyblockCollectionEntry> Entries { get; init; } = StaticMetadataDefaults.EmptyList<SkyblockCollectionEntry>();
}

/// <summary>
/// A collection entry definition.
/// </summary>
public class SkyblockCollectionEntry
{
	public string ItemId { get; init; } = string.Empty;
	public string Name { get; init; } = string.Empty;
	public string CategoryId { get; init; } = string.Empty;
	public SkyblockDisplayIcon Icon { get; init; } = new();
	public ReadOnlyCollection<int> Thresholds { get; init; } = StaticMetadataDefaults.EmptyList<int>();
	public int MaxTier { get; init; }
}

/// <summary>
/// A boss collection definition.
/// </summary>
public class SkyblockBossCollection
{
	public string BossId { get; init; } = string.Empty;
	public string Name { get; init; } = string.Empty;
	public SkyblockDisplayIcon Icon { get; init; } = new();
	public ReadOnlyCollection<int> Thresholds { get; init; } = StaticMetadataDefaults.EmptyList<int>();
}
