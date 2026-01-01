using Microsoft.EntityFrameworkCore;
using RepoAPI.Data;
using RepoAPI.Features.Items.Models;
using RepoAPI.Features.Output.Services;
using RepoAPI.Features.Wiki.Templates;
using SkyblockRepo.Models;

namespace RepoAPI.Features.Shops.Services;

[RegisterService<ShopLinkingService>(LifeTime.Scoped)]
public class ShopLinkingService(
    DataContext context,
    JsonWriteQueue writeQueue,
    ILogger<ShopLinkingService> logger)
{
    public async Task LinkShopsToItemsAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Linking shops to items...");

        var items = await context.SkyblockItems
            .Where(i => i.Latest)
            .ToDictionaryAsync(i => i.InternalId, i => i, ct);

        var shops = await context.SkyblockShops
            .Where(s => s.Latest)
            .OrderBy(s => s.InternalId)
            .ToListAsync(ct);

        var updatedItems = new HashSet<string>();

        foreach (var shop in shops)
        {
            if (shop.TemplateData?.Slots is null) continue;

            foreach (var slot in shop.TemplateData.Slots.Values)
            {
                if (slot.Output is null) continue;

                foreach (var output in slot.Output)
                {
                    if (output.ItemId is null) continue;
                    if (!items.TryGetValue(output.ItemId, out var item)) continue;
                    
                    item.SoldBy ??= [];
                        
                    // Check for duplicates - must check ALL entries with matching ID and cost
                    var isDuplicate = item.SoldBy.Any(s => 
                        s.Id == shop.InternalId && 
                        ParserUtils.DeepJsonEquals(s.Cost, slot.Cost));
                    
                    if (isDuplicate) continue;

                    item.SoldBy.Add(new SkyblockSoldBy
                    {
                        Id = shop.InternalId,
                        Name = shop.Name ?? shop.InternalId,
                        Cost = slot.Cost ?? [],
                        Amount = output.Amount
                    });
                        
                    updatedItems.Add(item.InternalId);
                }
            }
        }

        foreach (var itemId in updatedItems)
        {
            if (items.TryGetValue(itemId, out var item))
            {
                await writeQueue.QueueWriteAsync(new EntityWriteRequest(
                    Path: $"items/{item.InternalId}.json",
                    Data: item.ToOutputDto(),
                    KeepProperties: [ "recipes", "variants" ]
                ));
            }
        }

        logger.LogInformation("Linked shops to {Count} items.", updatedItems.Count);
    }
}
