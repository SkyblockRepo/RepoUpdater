using System.Collections.ObjectModel;

namespace SkyblockRepo.Models;

/// <summary>
/// Static reference data for Rift guide tasks and unlocks.
/// </summary>
public class SkyblockRiftData
{
	/// <summary>
	/// Gets Rift guide areas keyed by area id.
	/// </summary>
	public ReadOnlyDictionary<string, SkyblockRiftGuideArea> Areas { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<SkyblockRiftGuideArea>();

	/// <summary>
	/// Gets Rift timecharms keyed by id.
	/// </summary>
	public ReadOnlyDictionary<string, SkyblockRiftUnlockDefinition> Timecharms { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<SkyblockRiftUnlockDefinition>();

	/// <summary>
	/// Gets Rift eyes keyed by id.
	/// </summary>
	public ReadOnlyDictionary<string, SkyblockRiftUnlockDefinition> Eyes { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<SkyblockRiftUnlockDefinition>();

	/// <summary>
	/// Gets the total Enigma Soul count represented by the static dataset.
	/// </summary>
	public int EnigmaSoulCount { get; internal set; }

	/// <summary>
	/// Gets the maximum McGrubber stack count represented by the static dataset.
	/// </summary>
	public int MaxGrubberStacks { get; internal set; }
}

/// <summary>
/// A Rift guide area.
/// </summary>
public class SkyblockRiftGuideArea
{
	public string AreaId { get; init; } = string.Empty;
	public string Name { get; init; } = string.Empty;
	public ReadOnlyCollection<SkyblockRiftGuideTask> Tasks { get; init; } = StaticMetadataDefaults.EmptyList<SkyblockRiftGuideTask>();
}

/// <summary>
/// A Rift guide task.
/// </summary>
public class SkyblockRiftGuideTask
{
	public string? TaskId { get; init; }
	public string Name { get; init; } = string.Empty;
	public string Description { get; init; } = string.Empty;
	public string? WikiUrl { get; init; }
	public ReadOnlyCollection<SkyblockRiftGuideTask> Tasks { get; init; } = StaticMetadataDefaults.EmptyList<SkyblockRiftGuideTask>();
}

/// <summary>
/// A Rift timecharm or eye unlock definition.
/// </summary>
public class SkyblockRiftUnlockDefinition
{
	public string Id { get; init; } = string.Empty;
	public string Name { get; init; } = string.Empty;
	public SkyblockDisplayIcon Icon { get; init; } = new();
}
