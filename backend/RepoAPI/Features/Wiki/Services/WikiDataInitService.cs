using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using RepoAPI.Data;
using RepoAPI.Features.Items.Models;
using RepoAPI.Features.Pets.Models;
using RepoAPI.Features.Recipes.Models;

namespace RepoAPI.Features.Wiki.Services;

[RegisterService<WikiDataInitService>(LifeTime.Scoped)]
public class WikiDataInitService(
	IWikiApi wikiApi, 
	WikiDataService dataService,
	ILogger<WikiDataInitService> logger,
	DataContext context)
{
	public async Task InitializeWikiDataIfNeededAsync(CancellationToken ct)
	{
		var existingPets = await context.SkyblockPets
			.OrderBy(i => i.InternalId)
			.Take(1)
			.CountAsync(ct);
		
		if (existingPets == 0) {
			await InitializeWikiPets(ct);
		}
		
		var existingCount = await context.SkyblockItems
			.OrderBy(i => i.InternalId)
			.Take(1)
			.CountAsync(ct);
		
		if (existingCount == 0) {
			await InitializeWikiItems(ct);
		}
		
		var existingRecipes = await context.SkyblockRecipes
			.Where(r => r.Type == RecipeType.Crafting)
			.OrderBy(r => r.Hash)
			.Take(1)
			.CountAsync(ct);
		
		if (existingRecipes == 0) {
			await InitializeWikiRecipes(ct);
		}
	}
	
	public async Task InitializeWikiDataAsync(CancellationToken ct)
	{
		await InitializeWikiItems(ct);
		await InitializeWikiPets(ct);
		await InitializeWikiRecipes(ct);
	}
	
	private async Task InitializeWikiItems(CancellationToken ct)
	{
		const int batchSize = 50;
		var newItems = 0;
		var allItemIds = await dataService.GetAllWikiItemsAsync();

		for (var i = 0; i < allItemIds.Count; i += batchSize)
		{
			var batchIds = allItemIds.Skip(i).Take(batchSize).ToList();
			var wikiData = await dataService.BatchGetItemData(batchIds, true);
            
			foreach (var templateData in wikiData.Values)
			{
				var itemId = templateData?.Data?.InternalId;
				if (itemId is null) continue;

				var item = await context.SkyblockItems.FirstOrDefaultAsync(it => it.InternalId == itemId, ct);
				if (item is null)
				{
					item = new SkyblockItem
					{
						InternalId = itemId,
						Source = "HypixelWiki",
						NpcValue = int.TryParse(templateData?.Data?.Value, out var val) ? val : 0,
					};
                    
					newItems++;
					context.SkyblockItems.Add(item);
				}

				if (templateData != null) {
					item.PopulateTemplateData(templateData);
					
					if (templateData.Data?.RecipeTree?.ItemId is {} recipeTreeId && recipeTreeId != item.InternalId) {
						logger.LogInformation("Item {ItemId} has a RecipeTree pointing to a different item {RecipeTreeId}", item.InternalId, recipeTreeId);
						
						context.SkyblockItemRecipeLinks.Add(new SkyblockItemRecipeLink
						{
							InternalId = item.InternalId,
							RecipeId = recipeTreeId,
						});
					}
				}
			}
			
			// Wait for a moment to avoid hitting rate limits/overloading the wiki API
			await Task.Delay(300, ct);
		}
        
		await context.SaveChangesAsync(ct);

		if (newItems > 0) { 
			logger.LogInformation("Initialized wiki data for {NewItems} new items", newItems);
		}
	}
	
	private async Task InitializeWikiPets(CancellationToken ct)
	{
		const int batchSize = 50;
		var newPets = 0;
		var allPetIds = await dataService.GetAllWikiPetsAsync();

		for (var i = 0; i < allPetIds.Count; i += batchSize)
		{
			var batchIds = allPetIds.Skip(i).Take(batchSize).ToList();
			var wikiData = await dataService.BatchGetPetData(batchIds);
            
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
						Category = templateData?.Data?.AdditionalProperties.GetValueOrDefault("Category") as string,
						RawTemplate = templateData?.Wikitext,
						TemplateData = templateData?.Data,
					};
                    
					newPets++;
					context.SkyblockPets.Add(pet);
				}

				if (pet.TemplateData != null) continue;
				pet.RawTemplate = templateData?.Wikitext;
				pet.TemplateData = templateData?.Data;
			}
		}
        
		await context.SaveChangesAsync(ct);

		if (newPets > 0) { 
			logger.LogInformation("Initialized wiki data for {NewPets} new pets", newPets);
		}
	}
	
	private async Task InitializeWikiRecipes(CancellationToken ct)
	{
		const int batchSize = 50;
		var allPetIds = await dataService.GetAllWikiRecipesAsync();

		var newRecipes = new List<SkyblockRecipe>();
		
		for (var i = 0; i < allPetIds.Count; i += batchSize)
		{
			var batchIds = allPetIds.Skip(i).Take(batchSize).ToList();
			var wikiData = await dataService.BatchGetRecipeData(batchIds);
            
			foreach (var recipeTemplateDto in wikiData.Values)
			{
				var recipes = recipeTemplateDto?.Data?.Recipes;
				if (recipes is null) continue;
				
				foreach (var recipe in recipes)
				{
					var newRecipe = new SkyblockRecipe()
					{
						Id = Guid.CreateVersion7(),
						Type = RecipeType.Crafting,
						Hash = recipe.Hash,
						ResultInternalId = recipe.Result?.ItemId,
						ResultQuantity = recipe.Result?.Quantity ?? 1,
					};

					foreach (var ingredient in recipe.Ingredients)
					{
						newRecipe.Ingredients.Add(new RecipeIngredient
						{
							RecipeId = newRecipe.Id,
							
							Slot = ingredient.Slot,
							InternalId = ingredient.ItemId,
							Quantity = ingredient.Quantity,
						});
					}
					
					newRecipes.Add(newRecipe);
				}
			}
		}

		if (newRecipes.Count == 0)
		{
			logger.LogError("No recipes found to initialize.");
			return;
		}
		
		var transaction = await context.Database.BeginTransactionAsync(ct);
		try
		{
			// Delete existing crafting recipes
			await context.SkyblockRecipes
				.Where(t => t.Type == RecipeType.Crafting)
				.ExecuteDeleteAsync(cancellationToken: ct);
			
			var existingItems = await context.SkyblockItems
				.ToDictionaryAsync(i => i.InternalId, ct);
			
			var existingLinks = await context.SkyblockItemRecipeLinks
				.ToDictionaryAsync(l => l.RecipeId, l => l.InternalId, cancellationToken: ct);
			
			// Ensure all ingredients exist as items
			foreach (var recipe in newRecipes)
			{
				if (recipe.ResultInternalId != null && !existingItems.ContainsKey(recipe.ResultInternalId))
				{
					if (existingLinks.TryGetValue(recipe.ResultInternalId, out var linkId))
					{
						logger.LogWarning("Pointing recipe for {Item} to {LinkId}", recipe.ResultInternalId, linkId);
						recipe.ResultInternalId = linkId;
						
						if (!existingItems.ContainsKey(recipe.ResultInternalId)) {
							await AddMissingItem(recipe.ResultInternalId);
						}
					} else {
						await AddMissingItem(recipe.ResultInternalId);
					}
				}
				
				foreach (var ingredient in recipe.Ingredients)
				{
					if (existingItems.ContainsKey(ingredient.InternalId)) continue;

					if (existingLinks.TryGetValue(ingredient.InternalId, out var linkId))
					{
						logger.LogWarning("Pointing ingredient id for {Item} to {LinkId}", recipe.ResultInternalId, linkId);
						ingredient.InternalId = linkId;
					}

					if (existingItems.ContainsKey(ingredient.InternalId)) continue;
					
					await AddMissingItem(ingredient.InternalId);
				}
				
				continue;
				async Task AddMissingItem(string itemId)
				{
					var newItem = new SkyblockItem
					{
						InternalId = itemId,
						Source = "HypixelWikiRecipe",
						NpcValue = 0,
					};
					existingItems[itemId] = newItem;
					context.SkyblockItems.Add(newItem);
					logger.LogInformation("Added missing item {Item} for {Recipe}", itemId, recipe);
					
					await context.SaveChangesAsync(ct);
				}
			}
			
			await context.SkyblockRecipes.AddRangeAsync(newRecipes, ct);
			await context.SaveChangesAsync(ct);
			
			await transaction.CommitAsync(ct);
		}
		catch (Exception ex)
		{
			await transaction.RollbackAsync(ct);
			logger.LogError(ex, "Failed to initialize recipes.");
			return;
		}
		
		if (newRecipes.Count > 0) { 
			logger.LogInformation("Initialized wiki data for {Recipes} recipes", newRecipes.Count);
		}
	}
}