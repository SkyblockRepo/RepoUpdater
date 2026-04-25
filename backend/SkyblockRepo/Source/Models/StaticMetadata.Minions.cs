using System.Collections.ObjectModel;

namespace SkyblockRepo.Models;

/// <summary>
/// Static reference data for minion definitions and slot thresholds.
/// </summary>
public class SkyblockMinionsData
{
	/// <summary>
	/// Gets minions keyed by generator id.
	/// </summary>
	public ReadOnlyDictionary<string, SkyblockMinionDefinition> ByGeneratorId { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<SkyblockMinionDefinition>();

	/// <summary>
	/// Gets minions keyed by base minion id.
	/// </summary>
	public ReadOnlyDictionary<string, SkyblockMinionDefinition> ByBaseId { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<SkyblockMinionDefinition>();

	/// <summary>
	/// Gets minion categories keyed by category id.
	/// </summary>
	public ReadOnlyDictionary<string, SkyblockMinionCategory> Categories { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<SkyblockMinionCategory>();

	/// <summary>
	/// Gets minion slot thresholds keyed by unique minions crafted.
	/// </summary>
	public ReadOnlyDictionary<int, int> SlotThresholds { get; internal set; } = StaticMetadataDefaults.EmptyIntDictionary<int>();

	/// <summary>
	/// Gets the maximum minion slot count represented by the static table.
	/// </summary>
	public int MaxSlots { get; internal set; }
}

/// <summary>
/// A minion category definition.
/// </summary>
public class SkyblockMinionCategory
{
	public string CategoryId { get; init; } = string.Empty;
	public string Name { get; init; } = string.Empty;
	public SkyblockDisplayIcon Icon { get; init; } = new();
}

/// <summary>
/// A minion definition.
/// </summary>
public class SkyblockMinionDefinition
{
	public string GeneratorId { get; init; } = string.Empty;
	public string BaseId { get; init; } = string.Empty;
	public string Name { get; init; } = string.Empty;
	public string CategoryId { get; init; } = string.Empty;
	public int MaxTier { get; init; }
	public SkyblockDisplayIcon Icon { get; init; } = new();
}
