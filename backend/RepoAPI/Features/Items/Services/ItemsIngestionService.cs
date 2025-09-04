using HypixelAPI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using RepoAPI.Data;
using RepoAPI.Features.Items.Models;
using RepoAPI.Features.Output.Services;
using RepoAPI.Features.Wiki.Services;
using RepoAPI.Features.Wiki.Templates;

namespace RepoAPI.Features.Items.Services;

[RegisterService<ItemsIngestionService>(LifeTime.Scoped)]
public class ItemsIngestionService(
    IHypixelApi hypixelApi,
    IItemService itemService,
    DataContext context,
    IWikiDataService wikiService,
    JsonWriteQueue writeQueue,
    WikiItemsIngestionService wikiItemsIngestionService,
    HybridCache hybridCache,
    ILogger<ItemsIngestionService> logger) 
{
    private static DateTimeOffset LastWikiFetch = DateTimeOffset.MinValue;
    
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

        var itemsFromApi = apiResponse.Content.Items;

        var existingItems = await itemService.GetItemsDictionaryAsync(CancellationToken.None);
        
        var initializationRun = existingItems.Count == 0;
        var newCount = 0;
        var updatedCount = 0;
        
        // Use a new list for items to be added to avoid modifying the context while iterating
        var itemsToAdd = new List<SkyblockItem>();

        foreach (var apiItem in itemsFromApi) {
            if (apiItem.Id is null) continue;
            
            if (existingItems.TryGetValue(apiItem.Id, out var existingItem)) {
                // Check if the item data has changed
                if (ParserUtils.DeepJsonEquals(apiItem, existingItem.Data))
                {
                    // TEMPORARY TO WRITE INITIAL FILES
                    await WriteChangesToFile(existingItem);
                    continue;
                }
                
                // Deprecate the old version
                await context.SkyblockItems
                    .Where(i => i.Id == existingItem.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(i => i.Latest, false));
                    
                // Create a new version with the updated data
                var newVersion = new SkyblockItem {
                    InternalId = apiItem.Id,
                    Name = apiItem.Name ?? apiItem.Id,
                    NpcValue = apiItem.NpcSellPrice,
                    Data = apiItem,
                    // Copy over data that isn't from the API, like wiki text
                    RawTemplate = existingItem.RawTemplate,
                    Lore = existingItem.Lore,
                    Source = "HypixelAPI",
                    Flags = existingItem.Flags,
                    Category = existingItem.Category,
                };
                    
                itemsToAdd.Add(newVersion);
                updatedCount++;
                
                await WriteChangesToFile(newVersion);
            } else {
                // Completely new item
                var newItem = new SkyblockItem {
                    InternalId = apiItem.Id,
                    Name = apiItem.Name ?? apiItem.Id,
                    NpcValue = apiItem.NpcSellPrice,
                    Data = apiItem,
                    Latest = true
                };
                
                // Only fetch from the wiki for new items if not the first run
                if (!initializationRun)
                {
                    var templateData = await wikiService.GetItemData(apiItem.Id);
                    newItem.PopulateTemplateData(templateData);
                }
                
                itemsToAdd.Add(newItem);
                newCount++;
                
                await WriteChangesToFile(newItem);
            }
        }
        
        if (itemsToAdd.Count != 0) {
            context.SkyblockItems.AddRange(itemsToAdd);
        }

        await context.SaveChangesAsync();
        
        if (newCount > 0 || updatedCount > 0) {
            logger.LogInformation(
                "Updated Skyblock items: {NewCount} new, {UpdatedCount} updated versions", newCount, updatedCount);
        }
        else {
            logger.LogInformation("No new or updated Skyblock items");
        }
        
        await hybridCache.GetOrCreateAsync(
            "wiki-items-exist",
            async c => {
                try {
                    await wikiItemsIngestionService.IngestItemsDataAsync(c);
                    return true;
                } catch {
                    return false;
                }
            },
            options: new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromHours(2),
                LocalCacheExpiration = TimeSpan.FromHours(2)
            });
    }

    private async Task WriteChangesToFile(SkyblockItem skyblockItem)
    {
        await writeQueue.QueueWriteAsync(new EntityWriteRequest(
            Path: $"items/{skyblockItem.InternalId}.json",
            Data: skyblockItem.ToOutputDto(),
            KeepProperties: [ "recipes" ]
        ));
    }
}