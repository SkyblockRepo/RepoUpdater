using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using RepoAPI.Data;
using RepoAPI.Features.Enchantments.Services;
using RepoAPI.Features.Pets.Models;
using RepoAPI.Features.Pets.Services;
using RepoAPI.Features.Recipes.Services;

namespace RepoAPI.Features.Wiki.Services;

[RegisterService<WikiDataInitService>(LifeTime.Scoped)]
public class WikiDataInitService(
	IWikiDataService dataService,
	RecipeIngestionService recipeIngestionService,
	EnchantmentIngestionService enchantmentIngestionService,
	PetsIngestionService petsIngestionService,
	HybridCache hybridCache)
{
	public async Task InitializeWikiDataIfNeededAsync(CancellationToken ct)
	{
		await petsIngestionService.FetchAndLoadDataAsync(ct);
		
		
		await hybridCache.GetOrCreateAsync(
			"pets-exist",
			async c => {
				try {
					await petsIngestionService.FetchAndLoadDataAsync(c);
					return true;
				} catch {
					return false;
				}
			},
			options: new HybridCacheEntryOptions
			{
				Expiration = TimeSpan.FromMinutes(10),
				LocalCacheExpiration = TimeSpan.FromMinutes(10)
			}, 
			cancellationToken: ct);

		await hybridCache.GetOrCreateAsync(
			"recipes-exist",
			async c => {
				try {
					await recipeIngestionService.FetchAndLoadDataAsync(c);
					return true;
				} catch {
					return false;
				}
			},
			options: new HybridCacheEntryOptions
			{
				Expiration = TimeSpan.FromMinutes(10),
				LocalCacheExpiration = TimeSpan.FromMinutes(10)
			}, 
			cancellationToken: ct);

		await hybridCache.GetOrCreateAsync(
			"enchantments-exist",
			async c => {
				try {
					await enchantmentIngestionService.FetchAndLoadDataAsync(c);
					return true;
				} catch {
					return false;
				}
			},
			options: new HybridCacheEntryOptions
			{
				Expiration = TimeSpan.FromMinutes(10),
				LocalCacheExpiration = TimeSpan.FromMinutes(10)
			}, 
			cancellationToken: ct);
	}
	
	public async Task InitializeWikiDataAsync(CancellationToken ct)
	{
		// await InitializeWikiItems(ct);
		await petsIngestionService.FetchAndLoadDataAsync(ct);
		await recipeIngestionService.FetchAndLoadDataAsync(ct);
		await enchantmentIngestionService.FetchAndLoadDataAsync(ct);
		await InitializeAttributeShards(ct);
	}
	
	private async Task InitializeAttributeShards(CancellationToken ct)
	{
		var response = await dataService.GetAttributeListAsync();
		var list = response.Attributes;
		
		// if (newItems > 0) { 
		// 	await context.SaveChangesAsync(ct);
		// 	logger.LogInformation("Initialized wiki data for {NewItems} new attribute shard items", newItems);
		// }
	}
}