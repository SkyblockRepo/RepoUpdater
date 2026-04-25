using System.Collections.ObjectModel;

namespace SkyblockRepo.Models;

/// <summary>
/// Static reference data for essence shop perks.
/// </summary>
public class SkyblockEssencePerksData
{
	/// <summary>
	/// Gets essence categories keyed by essence id.
	/// </summary>
	public ReadOnlyDictionary<string, SkyblockEssenceCategory> Categories { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<SkyblockEssenceCategory>();

	/// <summary>
	/// Gets perks keyed by perk id.
	/// </summary>
	public ReadOnlyDictionary<string, SkyblockEssencePerkDefinition> ByPerkId { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<SkyblockEssencePerkDefinition>();
}

/// <summary>
/// An essence category definition.
/// </summary>
public class SkyblockEssenceCategory
{
	public string EssenceId { get; init; } = string.Empty;
	public string Name { get; init; } = string.Empty;
	public ReadOnlyCollection<SkyblockEssencePerkDefinition> Perks { get; init; } = StaticMetadataDefaults.EmptyList<SkyblockEssencePerkDefinition>();
}

/// <summary>
/// A single essence perk definition.
/// </summary>
public class SkyblockEssencePerkDefinition
{
	public string PerkId { get; init; } = string.Empty;
	public string EssenceId { get; init; } = string.Empty;
	public string Name { get; init; } = string.Empty;
	public ReadOnlyCollection<int> Costs { get; init; } = StaticMetadataDefaults.EmptyList<int>();
	public int MaxLevel { get; init; }
}
