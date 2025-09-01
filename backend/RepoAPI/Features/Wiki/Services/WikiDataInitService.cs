using Microsoft.EntityFrameworkCore;
using RepoAPI.Core.Services;
using RepoAPI.Data;
using RepoAPI.Features.Enchantments.Models;
using RepoAPI.Features.Items.Models;
using RepoAPI.Features.Pets.Models;
using RepoAPI.Features.Recipes.Models;
using RepoAPI.Features.Recipes.Services;
using RepoAPI.Features.Wiki.Templates;

namespace RepoAPI.Features.Wiki.Services;

[RegisterService<WikiDataInitService>(LifeTime.Scoped)]
public class WikiDataInitService(
	IWikiDataService dataService,
	ILogger<WikiDataInitService> logger,
	RecipeIngestionService recipeIngestionService,
	DataContext context)
{
	public async Task InitializeWikiDataIfNeededAsync(CancellationToken ct)
	{
		var existingPets = await context.SkyblockPets
			.OrderBy(i => i.InternalId)
			.Take(1)
			.CountAsync(ct);
		
		if (existingPets == 0) {
			await InitializeWikiPets(ct);
		}
		
		var existingCount = await context.SkyblockItems
			.OrderBy(i => i.InternalId)
			.Take(1)
			.CountAsync(ct);
		
		if (existingCount == 0) {
			await InitializeWikiItems(ct);
		}
		
		var existingRecipes = await context.SkyblockRecipes
			.Where(r => r.Type == RecipeType.Crafting)
			.OrderBy(r => r.Hash)
			.Take(1)
			.CountAsync(ct);
		
		if (existingRecipes == 0) {
			await recipeIngestionService.FetchAndLoadDataAsync(ct);
		}
		
		var existingEnchantments = await context.SkyblockEnchantments
			.OrderBy(i => i.InternalId)
			.Take(1)
			.CountAsync(ct);
		
		if (existingEnchantments == 0) {
			await InitializeEnchantments(ct);
		}
		
		// await InitializeAttributeShards(ct);
	}
	
	public async Task InitializeWikiDataAsync(CancellationToken ct)
	{
		await InitializeWikiItems(ct);
		await InitializeWikiPets(ct);
		await recipeIngestionService.FetchAndLoadDataAsync(ct);
		await InitializeEnchantments(ct);
		await InitializeAttributeShards(ct);
	}
	
	private async Task InitializeWikiItems(CancellationToken ct)
	{
		const int batchSize = 50;
		var newItems = 0;
		var allItemIds = await dataService.GetAllWikiItemsAsync();

		for (var i = 0; i < allItemIds.Count; i += batchSize)
		{
			var batchIds = allItemIds.Skip(i).Take(batchSize).ToList();
			var wikiData = await dataService.BatchGetItemData(batchIds, true);
            
			foreach (var templateData in wikiData.Values)
			{
				var itemId = templateData?.Data?.InternalId;
				if (itemId is null) continue;

				var item = await context.SkyblockItems.FirstOrDefaultAsync(it => it.InternalId == itemId, ct);
				if (item is null)
				{
					item = new SkyblockItem
					{
						InternalId = itemId,
						Source = "HypixelWiki",
						NpcValue = int.TryParse(templateData?.Data?.Value, out var val) ? val : 0,
					};
                    
					newItems++;
					context.SkyblockItems.Add(item);
				}
				
				item.Name = item.Data?.Name ?? item.InternalId;
				
				if (templateData == null) continue;
				
				item.PopulateTemplateData(templateData);
					
				if (templateData.Data?.RecipeTree?.ItemId is {} recipeTreeId && recipeTreeId != item.InternalId) {
					logger.LogInformation("Item {ItemId} has a RecipeTree pointing to a different item {RecipeTreeId}", item.InternalId, recipeTreeId);
						
					if (await context.SkyblockItemRecipeLinks.FirstOrDefaultAsync(l => l.InternalId == item.InternalId && l.RecipeId == recipeTreeId, ct) is null) {
						try {
							context.SkyblockItemRecipeLinks.Add(new SkyblockItemRecipeLink
							{
								InternalId = item.InternalId,
								RecipeId = recipeTreeId,
							});
						} catch (Exception ex) {
							logger.LogError(ex, "Failed to add recipe link from {ItemId} to {RecipeTreeId}", item.InternalId, recipeTreeId);
						}
					}
				}
			}
			
			await context.SaveChangesAsync(ct);
			
			// Wait for a moment to avoid hitting rate limits/overloading the wiki API
			await Task.Delay(300, ct);
		}
		
		if (newItems > 0) { 
			logger.LogInformation("Initialized wiki data for {NewItems} new items", newItems);
		}
	}
	
	private async Task InitializeWikiPets(CancellationToken ct)
	{
		const int batchSize = 50;
		var newPets = 0;
		var allPetIds = await dataService.GetAllWikiPetsAsync();

		for (var i = 0; i < allPetIds.Count; i += batchSize)
		{
			var batchIds = allPetIds.Skip(i).Take(batchSize).ToList();
			var wikiData = await dataService.BatchGetPetData(batchIds);
            
			foreach (var templateData in wikiData.Values)
			{
				var petId = templateData?.Data?.InternalId;
				if (petId is null) continue;

				var pet = await context.SkyblockPets.FirstOrDefaultAsync(it => it.InternalId == petId, ct);
				if (pet is null)
				{
					pet = new SkyblockPet
					{
						InternalId = petId,
						Source = "HypixelWiki",
						Category = templateData?.Data?.AdditionalProperties.GetValueOrDefault("Category") as string,
						RawTemplate = templateData?.Wikitext,
						TemplateData = templateData?.Data,
					};
                    
					newPets++;
					context.SkyblockPets.Add(pet);
				}

				if (pet.TemplateData != null) continue;
				pet.RawTemplate = templateData?.Wikitext;
				pet.TemplateData = templateData?.Data;
			}
		}
        
		await context.SaveChangesAsync(ct);

		if (newPets > 0) { 
			logger.LogInformation("Initialized wiki data for {NewPets} new pets", newPets);
		}
	}
	
	private async Task InitializeEnchantments(CancellationToken ct)
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
					
					if (level != null && int.TryParse(level, out var levelValue))
					{
						if (levelDictionary.TryGetValue(levelValue, out var lore))
						{
							item.Lore = lore;
						}
					}
				}
				
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
	
	private async Task InitializeAttributeShards(CancellationToken ct)
	{
		var response = await dataService.GetAttributeListAsync();
		var list = response.Attributes;
		
		// if (newItems > 0) { 
		// 	await context.SaveChangesAsync(ct);
		// 	logger.LogInformation("Initialized wiki data for {NewItems} new attribute shard items", newItems);
		// }
	}
}