using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RepoAPI.Core.Models;
using RepoAPI.Data;
using RepoAPI.Features.Items.Models;
using RepoAPI.Features.Pets.Models;
using RepoAPI.Features.Recipes.Models;

namespace RepoAPI.Core;

public class ApplyChangesJob(IServiceProvider serviceProvider, ILogger<ApplyChangesJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ApplyChangesJob is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Checking for approved batches...");
            
            await using (var scope = serviceProvider.CreateAsyncScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DataContext>();
                
                // Find the first available batch that has been approved.
                var approvedBatch = await context.DataIngestionBatches
                    .FirstOrDefaultAsync(b => b.Status == IngestionStatus.Approved, stoppingToken);

                if (approvedBatch != null)
                {
                    await ProcessBatchAsync(approvedBatch, context, stoppingToken);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
        
        logger.LogInformation("ApplyChangesJob is stopping.");
    }

    private async Task ProcessBatchAsync(DataIngestionBatch batch, DataContext context, CancellationToken ct)
    {
        logger.LogInformation("Processing batch {BatchId}...", batch.Id);
        await using var transaction = await context.Database.BeginTransactionAsync(ct);

        try
        {
            var deprecations = await context.PendingDeprecations
                .Where(d => d.BatchId == batch.Id)
                .ToListAsync(ct);

            var deprecationsByType = deprecations.GroupBy(d => d.EntityType);

            foreach (var group in deprecationsByType)
            {
                var entityType = group.Key;
                var idsToDeprecate = group.Select(d => d.EntityIdToDeprecate).ToArray();

                if (idsToDeprecate.Length == 0) continue;
                
                var safeTableName = entityType switch
                {
                    "SkyblockItem" => "SkyblockItem",
                    "SkyblockPet" => "SkyblockPets",
                    "SkyblockRecipe" => "SkyblockRecipes",
                    "SkyblockEnchantment" => "SkyblockEnchantments",
                    _ => throw new InvalidOperationException($"Entity type {entityType} is not supported.")
                };

                // Deprecate the old versions by setting Latest = false
                await context.Database.ExecuteSqlAsync(
                    $"UPDATE \"{safeTableName}\" SET \"Latest\" = false WHERE \"Id\" = ANY({idsToDeprecate})",
                    ct);
            }
            
            var pendingChanges = await context.PendingEntityChanges
                .Where(c => c.BatchId == batch.Id)
                .ToListAsync(ct);

            foreach (var change in pendingChanges)
            {
                switch (change.EntityType)
                {
                    case "SkyblockItem":
                    {
                        var item = change.EntityData.Deserialize<SkyblockItem>();
                        if (item != null) context.SkyblockItems.Add(item);
                        break;
                    }
                    case "SkyblockPet":
                    {
                        var pet = change.EntityData.Deserialize<SkyblockPet>();
                        if (pet != null) context.SkyblockPets.Add(pet);
                        break;
                    }
                    case "SkyblockRecipe":
                    {
                        var recipe = change.EntityData.Deserialize<SkyblockRecipe>();
                        if (recipe != null) context.SkyblockRecipes.Add(recipe);
                        break;
                    }
                }
            }
            
            await context.SaveChangesAsync(ct);

            // Mark the batch as applied
            batch.Status = IngestionStatus.Applied;
            await context.SaveChangesAsync(ct);
            
            await transaction.CommitAsync(ct);
            logger.LogInformation("Successfully applied batch {BatchId}.", batch.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process batch {BatchId}. Rolling back.", batch.Id);
            await transaction.RollbackAsync(ct);
        }
    }
}