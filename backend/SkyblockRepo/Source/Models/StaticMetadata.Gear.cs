using System.Collections.ObjectModel;

namespace SkyblockRepo.Models;

/// <summary>
/// Static reference data for optional skill gear groupings.
/// </summary>
public class SkyblockGearData
{
	/// <summary>
	/// Gets farming gear groupings.
	/// </summary>
	public SkyblockGearCatalog Farming { get; internal set; } = new();

	/// <summary>
	/// Gets fishing gear groupings.
	/// </summary>
	public SkyblockGearCatalog Fishing { get; internal set; } = new();

	/// <summary>
	/// Gets mining gear groupings.
	/// </summary>
	public SkyblockGearCatalog Mining { get; internal set; } = new();
}

/// <summary>
/// A gear catalog keyed by category name.
/// </summary>
public class SkyblockGearCatalog
{
	/// <summary>
	/// Gets gear ids grouped by category.
	/// </summary>
	public ReadOnlyDictionary<string, ReadOnlyCollection<string>> Categories { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<ReadOnlyCollection<string>>();

	/// <summary>
	/// Gets all unique ids in the catalog.
	/// </summary>
	public ReadOnlyCollection<string> AllIds { get; internal set; } = StaticMetadataDefaults.EmptyList<string>();
}
