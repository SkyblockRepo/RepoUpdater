using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RepoAPI.Core.Models;
using RepoAPI.Core.Services;
using RepoAPI.Data;
using RepoAPI.Features.Output.Services;
using RepoAPI.Features.Recipes.Models;
using RepoAPI.Features.Wiki.Services;
using RepoAPI.Features.Wiki.Templates.RecipeTemplate;

namespace RepoAPI.Features.Recipes.Services;

[RegisterService<RecipeIngestionService>(LifeTime.Scoped)]
public class RecipeIngestionService(
	DataContext context,
	IWikiDataService wikiDataService,
	ILogger<RecipeIngestionService> logger,
	JsonWriteQueue writeQueue
) : IDataLoader
{
	// const int ChangeThreshold = 20;
	
	public Task InitializeAsync(CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public async Task FetchAndLoadDataAsync(CancellationToken ct = default)
	{
		logger.LogInformation("Starting wiki recipe ingestion...");
		
		var allRecipes = await FetchAllWikiRecipesAsync(ct);
		if (allRecipes.Count == 0)
		{
			logger.LogWarning("No recipes found from wiki to initialize.");
			return;
		}
		
		var existingRecipes = await context.SkyblockRecipes
			.Where(r => r.Latest && r.Type == RecipeType.Crafting)
			.ToDictionaryAsync(r => r.InternalId, ct);

		var newVersions = new List<IVersionedEntity>();
		var deprecationIds = new List<int>();

		foreach (var craftingRecipe in allRecipes)
		{
			var hash = craftingRecipe.Hash;
			var entity = MapToEntity(craftingRecipe, hash);
			if (existingRecipes.TryGetValue(hash, out var existingRecipe))
			{
				if (existingRecipe.Hash != hash)
				{
					newVersions.Add(entity);
					deprecationIds.Add(existingRecipe.Id);
				}
			}
			else
			{
				newVersions.Add(entity);
			}
		}

		if (newVersions.Count == 0)
		{
			logger.LogInformation("No new or updated wiki recipes found.");
			return;
		}
		
		var transaction = await context.Database.BeginTransactionAsync(ct);
		try
		{
			await context.Database.ExecuteSqlAsync(
				$"UPDATE \"SkyblockRecipes\" SET \"Latest\" = false WHERE \"Id\" = ANY({deprecationIds})",
				ct);
		
			await context.SkyblockRecipes.AddRangeAsync(newVersions.OfType<SkyblockRecipe>(), ct);
			await context.SaveChangesAsync(ct);
		
			await transaction.CommitAsync(ct);
		} catch (Exception ex)
		{
			logger.LogError(ex, "Error during recipe ingestion transaction.");
			await transaction.RollbackAsync(ct);
			throw;
		}


		// var batch = new DataIngestionBatch
		// {
		// 	Source = "HypixelWikiRecipes",
		// 	Status = IngestionStatus.InProgress
		// };
		// context.DataIngestionBatches.Add(batch);
		
		await WriteChangesToFiles(newVersions.OfType<SkyblockRecipe>().ToList());

		// // Create the pending change objects using the batch.Id
		// var pendingChanges = newVersions
		// 	.Select(v => CreatePendingChange(batch.Id, "SkyblockRecipe", v))
		// 	.ToList();
		//   
		// var pendingDeprecations = deprecationIds
		// 	.Select(id => new PendingDeprecation { BatchId = batch.Id, EntityIdToDeprecate = id, EntityType = "SkyblockRecipe" })
		// 	.ToList();
		//
		// context.PendingEntityChanges.AddRange(pendingChanges);
		// context.PendingDeprecations.AddRange(pendingDeprecations);
		//
		// if (pendingChanges.Count > ChangeThreshold)
		// {
		// 	batch.Status = IngestionStatus.PendingApproval;
		// 	logger.LogWarning("{Count} recipe changes detected, requires manual approval.", pendingChanges.Count);
		// 	// TODO: Trigger alert system here with batch.Id
		// }
		// else
		// {
		// 	batch.Status = IngestionStatus.Approved;
		// 	logger.LogInformation("{Count} recipe changes detected, auto-approved for processing.",
		// 		pendingChanges.Count);
		// }

		await context.SaveChangesAsync(ct);
		logger.LogInformation("{Count} recipe changes detected, auto-approved for processing.",
			newVersions.Count);
	}

	/// <summary>
	/// Fetches all recipe data from the wiki in batches to avoid overwhelming the API.
	/// </summary>
	private async Task<List<CraftingRecipe>> FetchAllWikiRecipesAsync(CancellationToken ct)
	{
		const int batchSize = 50;
		// Assumes a method that returns all the recipe page titles to be fetched
		var allRecipePageTitles = await wikiDataService.GetAllWikiRecipesAsync();

		var allParsedRecipes = new List<CraftingRecipe>();

		for (var i = 0; i < allRecipePageTitles.Count; i += batchSize)
		{
			var batchTitles = allRecipePageTitles.Skip(i).Take(batchSize).ToList();
			// Assumes a method that takes a batch of titles and returns parsed data
			var wikiDataBatch = await wikiDataService.BatchGetRecipeData(batchTitles);

			foreach (var recipeTemplateDto in wikiDataBatch.Values)
			{
				var recipes = recipeTemplateDto?.Data?.Recipes;
				if (recipes is null) continue;

				allParsedRecipes.AddRange(recipes);
			}
		}

		return allParsedRecipes;
	}

	private SkyblockRecipe MapToEntity(CraftingRecipe dto, string hash)
	{
		return new SkyblockRecipe
		{
			InternalId = hash,
			Hash = hash,
			Type = RecipeType.Crafting,
			Name = dto.Name,
			ResultInternalId = dto.Result?.ItemId,
			ResultQuantity = dto.Result?.Quantity ?? 1,
			Ingredients = dto.Ingredients.Select(i => new RecipeIngredient
			{
				Slot = i.Slot,
				InternalId = i.ItemId,
				Quantity = i.Quantity,
			}).ToList()
		};
	}

	private PendingEntityChange CreatePendingChange(int batchId, string entityType, IVersionedEntity entity)
	{
		return new PendingEntityChange
		{
			BatchId = batchId,
			EntityType = entityType,
			InternalId = entity.InternalId,
			EntityData = JsonSerializer.SerializeToDocument(entity, entity.GetType())
		};
	}
	
	private async Task WriteChangesToFiles(List<SkyblockRecipe> recipes)
	{
		var groupedByItem = recipes
			.Select(r => r.ToDto())
			.Where(r => r.ResultId != null)
			.GroupBy(r => r.ResultId!)
			.ToDictionary(g => g.Key, g => g.ToList());

		foreach (var (itemId, value) in groupedByItem)
		{
			await writeQueue.QueueWriteAsync(new EntityWriteRequest(
				Path: $"items/{itemId}.json",
				Data: new { recipes = value },
				MergeInto: true
			));
		}
	}
}