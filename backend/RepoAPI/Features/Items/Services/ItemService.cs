using RepoAPI.Data;
using RepoAPI.Features.Items.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using RepoAPI.Features.Recipes.Models;

namespace RepoAPI.Features.Items.Services;

public interface IItemService
{
	ValueTask<SkyblockItemDto?> GetItemByIdAsync(string id, CancellationToken ct);
	Task<List<SkyblockItemDto>> GetAllItemsAsync(CancellationToken ct, string? source = null);
	Task<Dictionary<string, SkyblockItem>> GetItemsDictionaryAsync(CancellationToken ct, string? source = null);
}

[RegisterService<IItemService>(LifeTime.Scoped)]
public class ItemService(DataContext context, HybridCache cache, IWebHostEnvironment environment) : IItemService
{
	public ValueTask<SkyblockItemDto?> GetItemByIdAsync(string id, CancellationToken ct) =>
		cache.GetOrCreateAsync(
			$"item-id-{id}",
			async c =>
			{
				return await context.SkyblockItems
					.AsNoTracking()
					.Include(i => i.Recipes)
					.SelectDto()
					.FirstOrDefaultAsync(s => s.InternalId == id, c);
			},
			options: new HybridCacheEntryOptions
			{
				Expiration = TimeSpan.FromMinutes(environment.IsDevelopment() ? 1 : 10),
				LocalCacheExpiration = TimeSpan.FromMinutes(environment.IsDevelopment() ? 1 : 5)
			}, 
			cancellationToken: ct);

	public async Task<List<SkyblockItemDto>> GetAllItemsAsync(CancellationToken ct, string? source = null)
	{
		var items = await GetItemsDictionaryAsync(ct, source);
		return items.Values.Select(i => i.ToDto()).ToList();
	}
	
	public async Task<Dictionary<string, SkyblockItem>> GetItemsDictionaryAsync(CancellationToken ct, string? source = null)
	{
		var query = context.SkyblockItems.AsQueryable().AsNoTracking();
		if (!string.IsNullOrEmpty(source))
		{
			query = query.Where(i => i.Source == source);
		}
		
		var items = await query
			.AsNoTracking()
			.ToDictionaryAsync(i => i.InternalId, ct);
		
		var recipes = await context.SkyblockRecipes
			.AsNoTracking()
			.Where(r => r.Latest && r.Type == RecipeType.Crafting)
			.OrderBy(r => r.InternalId)
			.ToListAsync(ct);
		
		foreach (var recipe in recipes)
		{
			if (recipe.ResultInternalId is null) continue;
			if (!items.TryGetValue(recipe.ResultInternalId, out var item)) continue;
			
			item.Recipes.Add(recipe);
		}

		return items;
	}
}