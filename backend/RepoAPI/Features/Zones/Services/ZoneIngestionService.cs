using Microsoft.EntityFrameworkCore;
using RepoAPI.Core.Services;
using RepoAPI.Data;
using RepoAPI.Features.Zones.Models;
using RepoAPI.Features.Output.Services;
using RepoAPI.Features.Wiki.Services;

namespace RepoAPI.Features.Zones.Services;

[RegisterService<ZoneIngestionService>(LifeTime.Scoped)]
public class ZoneIngestionService(
    DataContext context,
    IWikiDataService wikiService,
    JsonWriteQueue writeQueue,
    ILogger<ZoneIngestionService> logger) : IDataLoader
{
    public Task InitializeAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task FetchAndLoadDataAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Starting wiki zone ingestion...");
        
        const int batchSize = 50;
        var newEntities = 0;
        var updatedEntities = 0;
        var allNpcIds = await wikiService.GetAllWikiZonesAsync();

        if (allNpcIds.Count == 0)
        {
            logger.LogWarning("No zones found from wiki to initialize.");
            return;
        }
        
        logger.LogInformation("Fetched {Count} zones from wiki.", allNpcIds.Count);
        
        for (var i = 0; i < allNpcIds.Count; i += batchSize)
        {
            var batchIds = allNpcIds.Skip(i).Take(batchSize).ToList();
            var wikiData = await wikiService.BatchGetZoneData(batchIds);
            
            foreach (var templateData in wikiData.Values)
            {
                var zoneId = templateData?.Data?.InternalId;
                if (zoneId is null) continue;

                var zone = await context.SkyblockZones.FirstOrDefaultAsync(it => it.InternalId == zoneId, ct);
                if (zone is null)
                {
                    zone = new SkyblockZone
                    {
                        InternalId = zoneId,
                        Source = "HypixelWiki",
                        RawTemplate = templateData?.Wikitext,
                    };
                    
                    newEntities++;
                    context.SkyblockZones.Add(zone);
                }

                zone.RawTemplate = templateData?.Wikitext;
                zone.Name = templateData?.Data?.Name ?? zone.InternalId;
                updatedEntities++;
                
                await WriteChangesToFile(zone);
            }
        }
        
        await context.SaveChangesAsync(ct);

        if (newEntities > 0) { 
            logger.LogInformation("Initialized wiki data for {NewPets} new zones", newEntities);
        }
        
        if (updatedEntities > 0) { 
            logger.LogInformation("Updated wiki data for {UpdatedPets} zones", updatedEntities);
        }
    }
    
    private async Task WriteChangesToFile(SkyblockZone zone)
    {
        await writeQueue.QueueWriteAsync(new EntityWriteRequest(
            Path: $"zones/{zone.InternalId}.json",
            Data: zone.ToDto()
        ));
    }
}