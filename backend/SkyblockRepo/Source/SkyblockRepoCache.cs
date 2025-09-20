using System.Collections.ObjectModel;
using SkyblockRepo.Models;

namespace SkyblockRepo;

public class SkyblockRepoCache
{
	public Manifest? Manifest { get; set; }
	public ReadOnlyDictionary<string, SkyblockItemData> Items { get; set; } = new(new Dictionary<string, SkyblockItemData>());
	public ReadOnlyDictionary<string, SkyblockItemNameSearch> ItemNameSearch { get; set; } = new(new Dictionary<string, SkyblockItemNameSearch>());
	public ReadOnlyDictionary<string, SkyblockPetData> Pets { get; set; } = new(new Dictionary<string, SkyblockPetData>());
	
	/// <summary>
	/// Searches for an item by its ID or name. First checks for an exact ID match, then searches by name if no ID match is found.
	/// For name searches, it performs a case-insensitive substring match and returns the most fitting item.
	/// </summary>
	/// <param name="itemIdOrName"></param>
	/// <returns></returns>
	public SkyblockItemData? FindItem(string itemIdOrName)
	{
		itemIdOrName = itemIdOrName.Trim();
		
		if (string.IsNullOrWhiteSpace(itemIdOrName))
			throw new ArgumentException("Item ID or name must be provided.", nameof(itemIdOrName));

		// First, try to find by exact item ID
		if (Items.TryGetValue(itemIdOrName, out var itemById))
		{
			return itemById;
		}
		
		itemIdOrName = itemIdOrName.ToUpperInvariant();

		// If not found by ID, search by name (case-insensitive substring match)
		var matchingItems = ItemNameSearch.Values
			.Where(item => !string.IsNullOrWhiteSpace(item.Name) &&
			               (item.NameUpper.Contains(itemIdOrName) || item.IdToNameUpper.Contains(itemIdOrName)))
			.ToList();

		if (matchingItems.Count == 0)
		{
			if (itemIdOrName.Contains("BLOCK OF ")) {
				// Try making "BLOCK OF COAL" into "COAL BLOCK"
				return FindItem(itemIdOrName.Replace("BLOCK OF ", "") + " BLOCK");
			}
			if (itemIdOrName.Contains("WOOD")) {
				// Try making "OAK WOOD" into "OAK LOG"
				return FindItem(itemIdOrName.Replace("WOOD", "LOG"));
			}
		}

		// If multiple items match, select the one with the shortest name (most fitting)
		var bestMatch = matchingItems.OrderBy(item => item.Name!.Length).FirstOrDefault();
		return bestMatch is not null ? Items.GetValueOrDefault(bestMatch.InternalId) : null;
	}
}