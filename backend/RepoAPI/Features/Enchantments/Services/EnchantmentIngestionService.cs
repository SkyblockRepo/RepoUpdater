using Microsoft.EntityFrameworkCore;
using RepoAPI.Core.Services;
using RepoAPI.Data;
using RepoAPI.Features.Enchantments.Models;
using RepoAPI.Features.Items.Models;
using RepoAPI.Features.Output.Services;
using RepoAPI.Features.Wiki.Services;
using RepoAPI.Features.Wiki.Templates;

namespace RepoAPI.Features.Enchantments.Services;

[RegisterService<EnchantmentIngestionService>(LifeTime.Scoped)]
public class EnchantmentIngestionService(
	DataContext context,
	IWikiDataService dataService,
	JsonWriteQueue writeQueue,
	ILogger<EnchantmentIngestionService> logger
	) : IDataLoader
{
	public Task InitializeAsync(CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public async Task FetchAndLoadDataAsync(CancellationToken ct = default)
	{
		var enchantTemplate = await dataService.GetAllWikiEnchantmentsAsync();
		var newEnchants = 0;
		const int batchSize = 50;
		
		var existingEnchants = await context.SkyblockEnchantments
			.ToDictionaryAsync(e => e.InternalId, cancellationToken: ct);

		for (var i = 0; i < enchantTemplate.Count; i += batchSize)
		{
			if (ct.IsCancellationRequested) return;
			
			var batchIds = enchantTemplate.Skip(i).Take(batchSize).ToList();
			var wikiData = await dataService.BatchGetItemData(batchIds, true);
			
			foreach (var templateData in wikiData.Values)
			{
				var enchantId = templateData?.Data?.InternalId;
				if (enchantId is null) continue;

				if (!existingEnchants.TryGetValue(enchantId, out var enchant))
				{
					enchant = new SkyblockEnchantment
					{
						InternalId = enchantId,
						Source = "HypixelWiki",
					};
					
					newEnchants++;
					context.SkyblockEnchantments.Add(enchant);
				}

				if (templateData == null) continue;
				
				enchant.RawTemplate = templateData.Wikitext;
				var baseName = templateData.Data?.AdditionalProperties.GetValueOrDefault("base_name")?.ToString() ?? enchant.InternalId;
				
				var minLevel = templateData.Data?.AdditionalProperties.GetValueOrDefault("minimum_level");
				if (int.TryParse(minLevel?.ToString(), out var minLevelValue))
				{
					enchant.MinLevel = minLevelValue;
				}
				
				var maxLevel = templateData.Data?.AdditionalProperties.GetValueOrDefault("maximum_level");
				if (int.TryParse(maxLevel?.ToString(), out var maxLevelValue))
				{
					enchant.MaxLevel = maxLevelValue;
				}

				var enchantedBookItems = enchant.GetItemIds();
				if (enchantedBookItems.Count == 0) continue;
				
				var levelDictionary = ParserUtils.GetPropDictionaryFromSwitch(templateData.Data?.Lore ?? "")
					.Select((s, index) => new { s.Key, s.Value, Index = index })
					.ToDictionary(
						x => x.Key.TryParseRoman(out var intKey) ? intKey : -1, 
						x => ParserUtils.CleanLoreString(x.Value));
				
				foreach (var itemId in enchantedBookItems)
				{
					var item = await context.SkyblockItems.FirstOrDefaultAsync(it => it.InternalId == itemId, ct);
					var level = itemId.Split('_').LastOrDefault();
					
					if (item is null)
					{
						item = new SkyblockItem
						{
							InternalId = itemId,
							Source = "HypixelWikiEnchantment",
							NpcValue = 0,
						};
						
						context.SkyblockItems.Add(item);
					}

					item.Name = baseName + " " + level.ToRomanOrDefault();
					item.Flags = new ItemFlags()
					{
						Tradable = templateData.Data?.Tradable == "Yes",
						Auctionable = templateData.Data?.Auctionable == "Yes",
						Bazaarable = templateData.Data?.Bazaarable == "Yes",
						Enchantable = templateData.Data?.Enchantable == "Yes",
						Museumable = templateData.Data?.Museumable is not null && templateData.Data.Museumable != "No",
						Reforgeable = templateData.Data?.Reforgeable == "Yes",
						Soulboundable = templateData.Data?.Soulboundable == "Yes",
						Sackable = templateData.Data?.Sackable == "Yes"
					};;
					
					if (level != null && int.TryParse(level, out var levelValue))
					{
						if (levelDictionary.TryGetValue(levelValue, out var lore))
						{
							item.Lore = lore;
						}
					}
					
					await WriteItemChangesToFile(item);
				}
				
				await WriteEnchantmentChangesToFile(enchant);
				
				await context.SaveChangesAsync(ct);
				
				logger.LogInformation("Added {ItemCount} items for {Enchantment}", enchantedBookItems.Count, enchantId);
			}
			
			// Wait for a moment to avoid hitting rate limits/overloading the wiki API
			await Task.Delay(300, ct);
		}
		
		await context.SaveChangesAsync(ct);
		
		if (newEnchants > 0) { 
			logger.LogInformation("Initialized wiki data for {NewEnchants} new enchantments", newEnchants);
		}
	}
	
	private async Task WriteItemChangesToFile(SkyblockItem skyblockItem)
	{
		await writeQueue.QueueWriteAsync(new EntityWriteRequest(
			Path: $"items/{skyblockItem.InternalId}.json",
			Data: skyblockItem.ToOutputDto()
		));
	}
	
	private async Task WriteEnchantmentChangesToFile(SkyblockEnchantment enchantment)
	{
		await writeQueue.QueueWriteAsync(new EntityWriteRequest(
			Path: $"enchantments/{enchantment.InternalId}.json",
			Data: enchantment.ToDto()
		));
	}
}