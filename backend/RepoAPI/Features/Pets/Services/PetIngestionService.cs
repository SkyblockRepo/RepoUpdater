using Microsoft.EntityFrameworkCore;
using RepoAPI.Core.Services;
using RepoAPI.Data;
using RepoAPI.Features.Output.Services;
using RepoAPI.Features.Pets.Models;
using RepoAPI.Features.Wiki.Services;

namespace RepoAPI.Features.Pets.Services;

[RegisterService<PetsIngestionService>(LifeTime.Scoped)]
public class PetsIngestionService(
    DataContext context,
    IWikiDataService wikiService,
    JsonWriteQueue writeQueue,
    ILogger<PetsIngestionService> logger) : IDataLoader
{
    public Task InitializeAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task FetchAndLoadDataAsync(CancellationToken ct = default)
    {
        const int batchSize = 50;
        var newPets = 0;
        var allPetIds = await wikiService.GetAllWikiPetsAsync();

        for (var i = 0; i < allPetIds.Count; i += batchSize)
        {
            var batchIds = allPetIds.Skip(i).Take(batchSize).ToList();
            var wikiData = await wikiService.BatchGetPetData(batchIds);
            
            foreach (var templateData in wikiData.Values)
            {
                var petId = templateData?.Data?.InternalId;
                if (petId is null) continue;

                var pet = await context.SkyblockPets.FirstOrDefaultAsync(it => it.InternalId == petId, ct);
                if (pet is null)
                {
                    pet = new SkyblockPet
                    {
                        InternalId = petId,
                        Source = "HypixelWiki",
                        RawTemplate = templateData?.Wikitext,
                    };
                    
                    newPets++;
                    context.SkyblockPets.Add(pet);
                }

                pet.RawTemplate = templateData?.Wikitext;
                pet.Name = templateData?.Data?.Name ?? pet.InternalId;
                pet.Category = templateData?.Data?.Category;
                // pet.TemplateData = templateData?.Data;
                
                await WriteChangesToFile(pet);
            }
        }
        
        await context.SaveChangesAsync(ct);

        if (newPets > 0) { 
            logger.LogInformation("Initialized wiki data for {NewPets} new pets", newPets);
        }
    }
    
    private async Task WriteChangesToFile(SkyblockPet pet)
    {
        await writeQueue.QueueWriteAsync(new EntityWriteRequest(
            Path: $"pets/{pet.InternalId}.json",
            Data: pet.ToDto()
        ));
    }
}