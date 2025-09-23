using EliteFarmers.HypixelAPI;
using EliteFarmers.HypixelAPI.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using RepoAPI.Data;
using RepoAPI.Features.Items.Models;
using RepoAPI.Features.Output.Services;
using RepoAPI.Features.Wiki.Services;
using RepoAPI.Features.Wiki.Templates;
using SkyblockRepo;

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
    ISkyblockRepoClient skyblockRepoClient,
    ILogger<ItemsIngestionService> logger) 
{
    public async Task IngestItemsDataAsync() {
        var apiResponse = await hypixelApi.FetchItemsAsync();
        var bazaarResponse = await hypixelApi.FetchBazaarAsync();
        
        if (!apiResponse.IsSuccessStatusCode || apiResponse.Content is not { Success: true }) {
            var errorContent = apiResponse.Error != null
                ? apiResponse.Error.ToString()
                : "Unknown error";
            logger.LogError("Failed to fetch skyblock item data. Status: {StatusCode}. Error: {Error}",
                apiResponse.StatusCode, errorContent);
            return;
        }
        
        var bzItems = bazaarResponse.Content?.Products ?? new Dictionary<string, BazaarItem>();

        var itemsFromApi = apiResponse.Content.Items;

        var existingItems = await itemService.GetItemsDictionaryAsync(CancellationToken.None);
        
        var initializationRun = existingItems.Count == 0;
        var newCount = 0;
        var updatedCount = 0;
        
        // Use a new list for items to be added to avoid modifying the context while iterating
        var itemsToAdd = new List<SkyblockItem>();
        
        foreach (var apiItem in itemsFromApi) {
            if (apiItem.Id is null) continue;
            
            if (apiItem.Skin is null) {
                var existingRepoItem = skyblockRepoClient.FindItem(apiItem.Id);
                if (existingRepoItem?.Data?.Skin is not null)
                {
                    apiItem.Skin = new ItemSkin()
                    {
                        Value = existingRepoItem.Data.Skin.Value,
                        Signature = existingRepoItem.Data.Skin.Signature
                    };
                }
            }
            
            if (existingItems.TryGetValue(apiItem.Id, out var existingItem)) {
                // Check if the item data has changed
                if (existingItem.Data is null || ParserUtils.DeepJsonEquals(apiItem, existingItem.Data)) {
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
                    Source = existingItem.Source ?? "HypixelAPI",
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
                existingItems.Add(apiItem.Id, newItem);
                newCount++;
                
                await WriteChangesToFile(newItem);
            }
        }
        
        foreach (var bzItemId in bzItems.Keys) {
            if (existingItems.TryGetValue(bzItemId, out var existing))
            {
                await WriteChangesToFile(existing);
                continue;
            }
            
            // New bazaar item not in the main item list
            var newItem = new SkyblockItem
            {
                InternalId = bzItemId,
                Name = bzItemId,
                NpcValue = 0,
                Flags = new ItemFlags() {
                    Bazaarable = true
                },
                Data = null,
                Latest = true,
                Source = "HypixelBazaar",
            };
            
            itemsToAdd.Add(newItem);
            newCount++;
                
            await WriteChangesToFile(newItem);
        }
        
        foreach (var existingItem in existingItems.Values)
        {
            if (existingItem.Data?.Skin is not null) continue;
            var repoItem = skyblockRepoClient.FindItem(existingItem.InternalId);
            if (repoItem?.Data?.Skin is null) continue;

            existingItem.Data ??= new ItemResponse();
            existingItem.Data.Id ??= existingItem.InternalId;
            existingItem.Data.Skin = new ItemSkin()
            {
                Value = repoItem.Data.Skin.Value,
                Signature = repoItem.Data.Skin.Signature
            };
            
            context.SkyblockItems.Update(existingItem);
            await WriteChangesToFile(existingItem);
            updatedCount++;
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