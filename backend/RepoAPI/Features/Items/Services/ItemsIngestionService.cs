using HypixelAPI;
using Microsoft.EntityFrameworkCore;
using RepoAPI.Data;
using RepoAPI.Features.Items.Models;
using RepoAPI.Features.Wiki.Services;

namespace RepoAPI.Features.Items.Services;

[RegisterService<ItemsIngestionService>(LifeTime.Scoped)]
public class ItemsIngestionService(
    IHypixelApi hypixelApi,
    DataContext context,
    WikiDataService wikiService,
    ILogger<ItemsIngestionService> logger) 
{
    public async Task IngestItemsDataAsync() {
        var apiResponse = await hypixelApi.FetchItems();
        
        if (!apiResponse.IsSuccessStatusCode || apiResponse.Content is not { Success: true }) {
            var errorContent = apiResponse.Error != null
                ? apiResponse.Error.ToString()
                : "Unknown error";
            logger.LogError("Failed to fetch skyblock item data. Status: {StatusCode}. Error: {Error}",
                apiResponse.StatusCode, errorContent);
            return;
        }

        var items = apiResponse.Content.Items;

        var existingItems = await context.SkyblockItems
            .ToDictionaryAsync(p => p.InternalId);
        
        var initializationRun = existingItems.Count == 0;

        var newCount = 0;
        var updatedCount = 0;

        foreach (var item in items) {
            if (item.Id is null) continue;
            
            if (existingItems.TryGetValue(item.Id, out var skyblockItem)) {
                // Update existing record
                skyblockItem.NpcValue = item.NpcSellPrice;
                skyblockItem.Data = item;
                updatedCount++;
            }
            else {
                // Insert new record
                var newItem = new SkyblockItem {
                    InternalId = item.Id,
                    NpcValue = item.NpcSellPrice,
                    Data = item,
                };
                
                if (!initializationRun)
                {
                    var templateData = await wikiService.GetItemData(item.Id);
                    newItem.PopulateTemplateData(templateData);
                }
                
                context.SkyblockItems.Add(newItem);
                newCount++;
            }
        }

        if (newCount > 0 || updatedCount > 0) {
            await context.SaveChangesAsync();
            logger.LogInformation(
                "Updated Skyblock items: {NewCount} new, {UpdatedCount} updated", newCount, updatedCount);
        }
        else {
            logger.LogInformation("No updated Skyblock items");
        }
        
        if (!initializationRun) return;
        
        logger.LogInformation("Initial run, loading item templates from wiki...");
        var startTime = DateTime.UtcNow;
        await InitializeWikiData();
        var duration = DateTime.UtcNow - startTime;
        logger.LogInformation("Item templates loaded in {Duration}", duration);
    }

    private async Task InitializeWikiData()
    {
        const int batchSize = 50;
        var newItems = 0;
        var allItemIds = await wikiService.GetAllWikiItemsAsync();

        for (var i = 0; i < allItemIds.Count; i += batchSize)
        {
            var batchIds = allItemIds.Skip(i).Take(batchSize).ToList();
            var wikiData = await wikiService.BatchGetItemData(batchIds, true);
            
            foreach (var templateData in wikiData.Values)
            {
                var itemId = templateData?.Data?.InternalId;
                if (itemId is null) continue;

                var item = await context.SkyblockItems.FindAsync(itemId);
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

                item.PopulateTemplateData(templateData);
            }
        }
        
        await context.SaveChangesAsync();

        if (newItems > 0) { 
            logger.LogInformation("Initialized wiki data for {NewItems} new items", newItems);
        }
    }
}