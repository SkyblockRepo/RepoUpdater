using Microsoft.EntityFrameworkCore;
using RepoAPI.Data;
using RepoAPI.Features.Items.Models;
using RepoAPI.Features.Pets.Models;

namespace RepoAPI.Features.Wiki.Services;

[RegisterService<WikiDataInitService>(LifeTime.Scoped)]
public class WikiDataInitService(
	IWikiApi wikiApi, 
	WikiDataService dataService,
	ILogger<WikiDataInitService> logger,
	DataContext context)
{
	public async Task InitializeWikiDataIfNeededAsync(CancellationToken ct)
	{
		var existingCount = await context.SkyblockItems
			.OrderBy(i => i.InternalId)
			.Take(1)
			.CountAsync(ct);
		
		if (existingCount == 0) {
			await InitializeWikiItems(ct);
		}
		
		var existingPets = await context.SkyblockPets
			.OrderBy(i => i.InternalId)
			.Take(1)
			.CountAsync(ct);
		
		if (existingPets == 0) {
			await InitializeWikiPets(ct);
		}
	}
	
	public async Task InitializeWikiDataAsync(CancellationToken ct)
	{
		await InitializeWikiItems(ct);
		await InitializeWikiPets(ct);
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

				if (templateData != null) {
					item.PopulateTemplateData(templateData);
				}
			}
		}
        
		await context.SaveChangesAsync(ct);

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
}