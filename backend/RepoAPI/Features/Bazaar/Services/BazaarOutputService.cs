using Microsoft.EntityFrameworkCore;
using RepoAPI.Data;
using RepoAPI.Features.Output.Services;

namespace RepoAPI.Features.Bazaar.Services;

[RegisterService<BazaarOutputService>(LifeTime.Scoped)]
public class BazaarOutputService(
    DataContext context,
    JsonWriteQueue writeQueue,
    ILogger<BazaarOutputService> logger)
{
    public async Task GenerateBazaarOutputAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Generating Bazaar output...");

        var bazaarItems = await context.SkyblockItems
            .Where(i => i.Latest && i.Flags.Bazaarable)
            .Select(i => new { i.InternalId, i.Name, i.Category })
            .ToListAsync(ct);

        var bazaarShops = await context.SkyblockShops
            .Where(s => s.Latest && s.InternalId.StartsWith("NPC_BAZAAR_"))
            .ToListAsync(ct);

        var itemCategories = new Dictionary<string, string>();

        foreach (var shop in bazaarShops)
        {
            if (shop.TemplateData?.Slots is null) continue;

            var categoryName = shop.Name?.Split('➜').LastOrDefault()?.Trim();
            if (string.IsNullOrWhiteSpace(categoryName) || categoryName == "Bazaar") continue;

            foreach (var slot in shop.TemplateData.Slots.Values)
            {
                if (string.IsNullOrWhiteSpace(slot.Name)) {
                    continue;
                }

                // Strip color codes
                var slotName = System.Text.RegularExpressions.Regex.Replace(slot.Name, "[&§][0-9a-fk-or]", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();

                // Find matching item by name
                var item = bazaarItems.FirstOrDefault(i => string.Equals(i.Name, slotName, StringComparison.OrdinalIgnoreCase));
                if (item != null)
                {
                    itemCategories[item.InternalId] = categoryName;
                }
            }
        }

        var categorizedItems = bazaarItems
            .GroupBy(i => itemCategories.GetValueOrDefault(i.InternalId) ?? "Uncategorized")
            .ToDictionary(
                g => g.Key,
                g => g.Select(i => i.InternalId).ToList()
            );

        // Not ready yet
        // await writeQueue.QueueWriteAsync(new EntityWriteRequest(
        //     Path: "misc/bazaar.json",
        //     Data: categorizedItems
        // ));

        logger.LogInformation("Generated Bazaar output with {Count} items across {CategoryCount} categories.", 
            bazaarItems.Count, categorizedItems.Count);
    }
}
