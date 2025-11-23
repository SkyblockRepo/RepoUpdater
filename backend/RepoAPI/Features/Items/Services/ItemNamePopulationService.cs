using EliteFarmers.HypixelAPI.DTOs;
using Microsoft.EntityFrameworkCore;
using RepoAPI.Data;
using RepoAPI.Features.Items.Models;
using RepoAPI.Features.Wiki.Templates;
using RepoAPI.Util;

namespace RepoAPI.Features.Items.Services;

/// <summary>
/// Service to populate missing item names in the database.
/// For enchantment items, looks up the enchantment name.
/// For other items, converts the internal ID to title case.
/// </summary>
[RegisterService<ItemNamePopulationService>(LifeTime.Scoped)]
public class ItemNamePopulationService(
	DataContext context,
	ILogger<ItemNamePopulationService> logger)
{
	public async Task PopulateMissingNamesAsync(CancellationToken ct = default)
	{
		logger.LogInformation("Populating missing item names...");
		
		// Find items that don't have a Data property or have Data but no name
		var itemsMissingNames = await context.SkyblockItems
			.Where(i => i.Latest && (i.Data == null || i.Data.Name == null || i.Data.Name == ""))
			.ToListAsync(ct);
		
		if (itemsMissingNames.Count == 0)
		{
			logger.LogInformation("No items missing names.");
			return;
		}
		
		logger.LogInformation("Found {Count} items missing names", itemsMissingNames.Count);
		
		// Get all enchantments for lookup
		var enchantments = await context.SkyblockEnchantments
			.AsNoTracking()
			.Where(e => e.Latest)
			.ToDictionaryAsync(e => e.InternalId, e => e.Name, ct);
		
		var updatedCount = 0;
		
		foreach (var item in itemsMissingNames)
		{
			var name = GetItemName(item, enchantments);
			
			// Create or update the Data property
			if (item.Data == null)
			{
				item.Data = new ItemResponse
				{
					Id = item.InternalId,
					Name = name
				};
			}
			else
			{
				item.Data.Name = name;
			}
			
			updatedCount++;
		}
		
		await context.SaveChangesAsync(ct);
		logger.LogInformation("Populated names for {Count} items", updatedCount);
	}
	
	private static string GetItemName(SkyblockItem item, Dictionary<string, string?> enchantments)
	{
		// Check if this is an enchantment item
		if (!item.Source.Contains("Enchantment", StringComparison.OrdinalIgnoreCase))
			return item.InternalId.ToTitleCase();
		
		var parts = item.InternalId.Split('_');
		if (parts.Length < 3 || parts[0] != "ENCHANTMENT") return item.InternalId.ToTitleCase();
		
		// Get the enchantment ID (everything between ENCHANTMENT_ and the level number)
		var enchantId = string.Join("_", parts.Skip(1).Take(parts.Length - 2));
		var level = parts[^1]; // Last part is the level
				
		if (enchantments.TryGetValue(enchantId, out var enchantName) && !string.IsNullOrEmpty(enchantName))
		{
			// Convert level to roman numeral
			if (int.TryParse(level, out var levelNum))
			{
				return $"{enchantName} {levelNum.ToRomanOrDefault()}";
			}
			return enchantName;
		}

		// Fall back to converting the ID to title case
		return item.InternalId.ToTitleCase();
	}
}
