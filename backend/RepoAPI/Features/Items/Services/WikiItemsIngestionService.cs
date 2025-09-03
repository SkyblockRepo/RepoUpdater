using HypixelAPI;
using Microsoft.EntityFrameworkCore;
using RepoAPI.Data;
using RepoAPI.Features.Items.Models;
using RepoAPI.Features.Output.Services;
using RepoAPI.Features.Wiki.Services;
using RepoAPI.Features.Wiki.Templates;

namespace RepoAPI.Features.Items.Services;

[RegisterService<WikiItemsIngestionService>(LifeTime.Scoped)]
public class WikiItemsIngestionService(
    DataContext context,
    IWikiDataService dataService,
    JsonWriteQueue writeQueue,
    ILogger<WikiItemsIngestionService> logger) 
{
    public async Task IngestItemsDataAsync(CancellationToken ct = default) {
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
				
				item.Recipes = await context.SkyblockRecipes
					.Where(r => r.Latest && r.ResultInternalId == item.InternalId)
					.OrderBy(r => r.InternalId)
					.ToListAsync(ct);
				
				item.Name = item.Data?.Name ?? item.InternalId;

				if (templateData == null)
				{
					await WriteChangesToFile(item);
					continue;
				}
				
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
				
				await WriteChangesToFile(item);
			}
			
			await context.SaveChangesAsync(ct);
			
			// Wait for a moment to avoid hitting rate limits/overloading the wiki API
			await Task.Delay(300, ct);
		}
		
		if (newItems > 0) { 
			logger.LogInformation("Initialized wiki data for {NewItems} new items", newItems);
		}
    }

    private async Task WriteChangesToFile(SkyblockItem skyblockItem)
    {
        await writeQueue.QueueWriteAsync(new EntityWriteRequest(
            Path: $"items/{skyblockItem.InternalId}.json",
            Data: skyblockItem.ToOutputDto()
            // KeepProperties: [ "recipes" ]
        ));
    }
}