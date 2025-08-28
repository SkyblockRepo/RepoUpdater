using RepoAPI.Data;
using RepoAPI.Features.Items.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace RepoAPI.Features.Items.Services;

public interface IItemService
{
	ValueTask<SkyblockItemDto?> GetItemByIdAsync(string id, CancellationToken ct);
	Task<List<SkyblockItemDto>> GetAllItemsAsync(CancellationToken ct, string? source = null);
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
		var query = context.SkyblockItems.AsQueryable().AsNoTracking();
		if (!string.IsNullOrEmpty(source))
		{
			query = query.Where(i => i.Source == source);
		}
		return await query.Include(i => i.Recipes)
			// .Where(i => i.RawTemplate == null)
			.SelectDto()
			.ToListAsync(ct);
	}
}