using Microsoft.EntityFrameworkCore;
using RepoAPI.Core.Services;
using RepoAPI.Data;
using RepoAPI.Features.Shops.Models;
using RepoAPI.Features.Output.Services;
using RepoAPI.Features.Wiki.Services;

namespace RepoAPI.Features.Shops.Services;

[RegisterService<ShopIngestionService>(LifeTime.Scoped)]
public class ShopIngestionService(
    DataContext context,
    IWikiDataService wikiService,
    JsonWriteQueue writeQueue,
    ILogger<ShopIngestionService> logger) : IDataLoader
{
    public Task InitializeAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task FetchAndLoadDataAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Starting wiki shop ingestion...");
        
        const int batchSize = 50;
        var newEntities = 0;
        var updatedEntities = 0;
        var allNpcIds = await wikiService.GetAllWikiShops();

        if (allNpcIds.Count == 0)
        {
            logger.LogWarning("No shops found from wiki to initialize.");
            return;
        }
        
        logger.LogInformation("Fetched {Count} shops from wiki.", allNpcIds.Count);
        
        for (var i = 0; i < allNpcIds.Count; i += batchSize)
        {
            var batchIds = allNpcIds.Skip(i).Take(batchSize).ToList();
            var wikiData = await wikiService.BatchGetShopData(batchIds);
            
            foreach (var templateData in wikiData.Values)
            {
                var shopId = templateData?.Data?.InternalId;
                if (shopId is null) continue;

                var shop = await context.SkyblockShops.FirstOrDefaultAsync(it => it.InternalId == shopId, ct);
                if (shop is null)
                {
                    shop = new SkyblockShop
                    {
                        InternalId = shopId,
                        Source = "HypixelWiki",
                        RawTemplate = templateData?.Wikitext,
                    };
                    
                    newEntities++;
                    context.SkyblockShops.Add(shop);
                }

                shop.RawTemplate = templateData?.Wikitext;
                shop.Name = templateData?.Data?.Name ?? shop.InternalId;
                updatedEntities++;
                
                await WriteChangesToFile(shop);
            }
        }
        
        await context.SaveChangesAsync(ct);

        if (newEntities > 0) { 
            logger.LogInformation("Initialized wiki data for {NewShops} new shops", newEntities);
        }
        
        if (updatedEntities > 0) { 
            logger.LogInformation("Updated wiki data for {UpdatedShops} shops", updatedEntities);
        }
    }
    
    private async Task WriteChangesToFile(SkyblockShop shop)
    {
        await writeQueue.QueueWriteAsync(new EntityWriteRequest(
            Path: $"shops/{shop.InternalId}.json",
            Data: shop.ToDto()
        ));
    }
}