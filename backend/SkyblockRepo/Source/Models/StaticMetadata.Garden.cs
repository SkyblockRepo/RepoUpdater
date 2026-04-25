using System.Collections.ObjectModel;

namespace SkyblockRepo.Models;

/// <summary>
/// Static reference data for Garden progression, visitors, plots, and composter metadata.
/// </summary>
public class SkyblockGardenData
{
	/// <summary>
	/// Gets the cumulative Garden experience table.
	/// </summary>
	public ReadOnlyCollection<int> ExperienceTable { get; internal set; } = StaticMetadataDefaults.EmptyList<int>();

	/// <summary>
	/// Gets crop milestone thresholds keyed by crop id.
	/// </summary>
	public ReadOnlyDictionary<string, ReadOnlyCollection<int>> CropMilestones { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<ReadOnlyCollection<int>>();

	/// <summary>
	/// Gets visitors keyed by visitor id.
	/// </summary>
	public ReadOnlyDictionary<string, SkyblockGardenVisitorDefinition> Visitors { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<SkyblockGardenVisitorDefinition>();

	/// <summary>
	/// Gets plots keyed by plot id.
	/// </summary>
	public ReadOnlyDictionary<string, SkyblockGardenPlotDefinition> Plots { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<SkyblockGardenPlotDefinition>();

	/// <summary>
	/// Gets plot unlock costs keyed by plot group id.
	/// </summary>
	public ReadOnlyDictionary<string, ReadOnlyCollection<SkyblockGardenCost>> PlotCosts { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<ReadOnlyCollection<SkyblockGardenCost>>();

	/// <summary>
	/// Gets barn skins keyed by skin id.
	/// </summary>
	public ReadOnlyDictionary<string, SkyblockGardenBarnSkin> BarnSkins { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<SkyblockGardenBarnSkin>();

	/// <summary>
	/// Gets crop upgrade thresholds.
	/// </summary>
	public ReadOnlyCollection<int> CropUpgradeThresholds { get; internal set; } = StaticMetadataDefaults.EmptyList<int>();

	/// <summary>
	/// Gets composter upgrades keyed by upgrade id.
	/// </summary>
	public ReadOnlyDictionary<string, ReadOnlyCollection<SkyblockGardenUpgradeLevel>> ComposterUpgrades { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<ReadOnlyCollection<SkyblockGardenUpgradeLevel>>();

	/// <summary>
	/// Gets composter tooltip templates keyed by upgrade id.
	/// </summary>
	public ReadOnlyDictionary<string, string> ComposterTooltipTemplates { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<string>();

	/// <summary>
	/// Gets greenhouse upgrades keyed by upgrade id.
	/// </summary>
	public ReadOnlyDictionary<string, ReadOnlyCollection<SkyblockGardenUpgradeLevel>> GreenhouseUpgrades { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<ReadOnlyCollection<SkyblockGardenUpgradeLevel>>();

	/// <summary>
	/// Gets greenhouse crop mutations keyed by item id.
	/// </summary>
	public ReadOnlyDictionary<string, SkyblockGardenMutationDefinition> Mutations { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<SkyblockGardenMutationDefinition>();

	/// <summary>
	/// Gets Garden tools keyed by resource id when upstream metadata is available.
	/// </summary>
	public ReadOnlyDictionary<string, SkyblockGardenToolDefinition> Tools { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<SkyblockGardenToolDefinition>();
}

/// <summary>
/// A Garden visitor definition.
/// </summary>
public class SkyblockGardenVisitorDefinition
{
	public string VisitorId { get; init; } = string.Empty;
	public string Name { get; init; } = string.Empty;
	public string Rarity { get; init; } = string.Empty;
	public SkyblockDisplayIcon Icon { get; init; } = new();
}

/// <summary>
/// A Garden plot definition.
/// </summary>
public class SkyblockGardenPlotDefinition
{
	public string PlotId { get; init; } = string.Empty;
	public string Name { get; init; } = string.Empty;
	public int X { get; init; }
	public int Y { get; init; }
	public int? PlotNumber { get; init; }
}

/// <summary>
/// A Garden plot or upgrade cost.
/// </summary>
public class SkyblockGardenCost
{
	public string ItemId { get; init; } = string.Empty;
	public int Amount { get; init; }
}

/// <summary>
/// A Garden barn skin definition.
/// </summary>
public class SkyblockGardenBarnSkin
{
	public string SkinId { get; init; } = string.Empty;
	public string Name { get; init; } = string.Empty;
	public string ItemId { get; init; } = string.Empty;
}

/// <summary>
/// A Garden upgrade level.
/// </summary>
public class SkyblockGardenUpgradeLevel
{
	public int Level { get; init; }
	public int Reward { get; init; }
	public int? CopperCost { get; init; }
	public ReadOnlyDictionary<string, int> ItemCosts { get; init; } = StaticMetadataDefaults.EmptyDictionary<int>();
	public string? TooltipTemplate { get; init; }
}

/// <summary>
/// A greenhouse mutation definition sourced from upstream metadata.
/// </summary>
public class SkyblockGardenMutationDefinition
{
	public string ItemId { get; init; } = string.Empty;
	public string Name { get; init; } = string.Empty;
	public string Rarity { get; init; } = string.Empty;
	public bool Analyzable { get; init; } = true;
	public SkyblockDisplayIcon Icon { get; init; } = new();
}

/// <summary>
/// A Garden tool definition for a crop or resource.
/// </summary>
public class SkyblockGardenToolDefinition
{
	public string ResourceId { get; init; } = string.Empty;
	public string DisplayName { get; init; } = string.Empty;
	public string ToolType { get; init; } = string.Empty;
	public ReadOnlyCollection<string> ItemIds { get; init; } = StaticMetadataDefaults.EmptyList<string>();
}
