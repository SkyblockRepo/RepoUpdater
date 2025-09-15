using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using RepoAPI.Data;
using RepoAPI.Features.Enchantments.Services;
using RepoAPI.Features.NPCs.Services;
using RepoAPI.Features.Pets.Services;
using RepoAPI.Features.Recipes.Services;
using RepoAPI.Features.Zones.Services;

namespace RepoAPI.Features.Wiki.Services;

[RegisterService<WikiDataInitService>(LifeTime.Scoped)]
public class WikiDataInitService(
	DataContext context,
	IWikiDataService dataService,
	RecipeIngestionService recipeIngestionService,
	EnchantmentIngestionService enchantmentIngestionService,
	PetsIngestionService petsIngestionService,
	NpcIngestionService npcIngestionService,
	ZoneIngestionService zoneIngestionService,
	HybridCache hybridCache)
{
	public async Task InitializeWikiDataIfNeededAsync(CancellationToken ct)
	{
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
		
		await hybridCache.GetOrCreateAsync(
			"npcs-exist",
			async c => {
				try
				{
					await npcIngestionService.FetchAndLoadDataAsync(c);
					return true;
				}
				catch
				{
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
		
		var petsExist = await context.SkyblockPets.AnyAsync(ct);
		if (!petsExist) {
			await petsIngestionService.FetchAndLoadDataAsync(ct);
		}
		
		var recipesExist = await context.SkyblockRecipes.AnyAsync(ct);
		if (!recipesExist) {
			await recipeIngestionService.FetchAndLoadDataAsync(ct);
		}
		
		var enchantsExist = await context.SkyblockEnchantments.AnyAsync(ct);
		if (!enchantsExist) {
			await enchantmentIngestionService.FetchAndLoadDataAsync(ct);
		}
		
		var npcsExist = await context.SkyblockNpcs.AnyAsync(ct);
		if (!npcsExist) {
			await npcIngestionService.FetchAndLoadDataAsync(ct);
		}
		
		var zonesExist = await context.SkyblockZones.AnyAsync(ct);
		if (!zonesExist) {
			await zoneIngestionService.FetchAndLoadDataAsync(ct);
		}
		// await InitializeAttributeShards(ct);
	}
	
	private async Task InitializeAttributeShards(CancellationToken ct)
	{
		var response = await dataService.GetAttributeListAsync();
		var list = response.Attributes;
		
		foreach (var shard in list)
		{
			var internalId = $"SHARD_{shard.ShardName.Replace(" ", "_").ToUpperInvariant()}";
			var existing = await context.SkyblockItems.FirstOrDefaultAsync(s => s.InternalId == internalId, cancellationToken: ct);
			if (existing is null)
			{
				continue;
			}
			
			// if (existing != null) continue;
			//
			// var newPet = new SkyblockPet
			// {
			// 	InternalId = shard.Id,
			// 	Name = shard.Name ?? shard.Id,
			// 	Source = "HypixelWiki",
			// 	Rarity = "UNCOMMON",
			// 	Type = "ATTRIBUTE_SHARD",
			// 	RawTemplate = shard.Wikitext
			// };
			//
			// await petsIngestionService.AddOrUpdatePetAsync(newPet, ct);
		}
		
		// if (newItems > 0) { 
		// 	await context.SaveChangesAsync(ct);
		// 	logger.LogInformation("Initialized wiki data for {NewItems} new attribute shard items", newItems);
		// }
	}
}