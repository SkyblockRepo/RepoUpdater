using Microsoft.EntityFrameworkCore;
using RepoAPI.Core.Services;
using RepoAPI.Data;
using RepoAPI.Features.NPCs.Models;
using RepoAPI.Features.Output.Services;
using RepoAPI.Features.Wiki.Services;

namespace RepoAPI.Features.NPCs.Services;

[RegisterService<NpcIngestionService>(LifeTime.Scoped)]
public class NpcIngestionService(
    DataContext context,
    IWikiDataService wikiService,
    JsonWriteQueue writeQueue,
    ILogger<NpcIngestionService> logger) : IDataLoader
{
    public Task InitializeAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task FetchAndLoadDataAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Starting wiki npc ingestion...");
        
        const int batchSize = 50;
        var newEntities = 0;
        var updatedEntities = 0;
        var allNpcIds = await wikiService.GetAllWikiNpcsAsync();

        if (allNpcIds.Count == 0)
        {
            logger.LogWarning("No npcs found from wiki to initialize.");
            return;
        }
        
        logger.LogInformation("Fetched {Count} npcs from wiki.", allNpcIds.Count);
        
        for (var i = 0; i < allNpcIds.Count; i += batchSize)
        {
            var batchIds = allNpcIds.Skip(i).Take(batchSize).ToList();
            var wikiData = await wikiService.BatchGetNpcData(batchIds);
            
            foreach (var templateData in wikiData.Values)
            {
                var npcId = templateData?.Data?.InternalId;
                if (npcId is null) continue;

                var npc = await context.SkyblockNpcs.FirstOrDefaultAsync(it => it.InternalId == npcId, ct);
                if (npc is null)
                {
                    npc = new SkyblockNpc
                    {
                        InternalId = npcId,
                        Source = "HypixelWiki",
                        RawTemplate = templateData?.Wikitext,
                    };
                    
                    newEntities++;
                    context.SkyblockNpcs.Add(npc);
                }

                npc.RawTemplate = templateData?.Wikitext;
                npc.Name = templateData?.Data?.Name ?? npc.InternalId;
                updatedEntities++;
                
                await WriteChangesToFile(npc);
            }
        }
        
        await context.SaveChangesAsync(ct);

        if (newEntities > 0) { 
            logger.LogInformation("Initialized wiki data for {NewPets} new npcs", newEntities);
        }
        
        if (updatedEntities > 0) { 
            logger.LogInformation("Updated wiki data for {UpdatedPets} npcs", updatedEntities);
        }
    }
    
    private async Task WriteChangesToFile(SkyblockNpc npc)
    {
        await writeQueue.QueueWriteAsync(new EntityWriteRequest(
            Path: $"npcs/{npc.InternalId}.json",
            Data: npc.ToDto()
        ));
    }
}