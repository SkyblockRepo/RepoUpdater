using RepoAPI.Data;
using RepoAPI.Features.Items.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace RepoAPI.Features.Items.Services;

[RegisterService<ItemService>(LifeTime.Scoped)]
public class ItemService(DataContext context, HybridCache cache)
{
	public ValueTask<SkyblockItem?> GetItemByIdAsync(string id, CancellationToken ct) =>
		cache.GetOrCreateAsync(
			$"item-id-{id}",
			async c =>
			{
				return await context.SkyblockItems.FirstOrDefaultAsync(s => s.InternalId == id, c);
			},
			options: new HybridCacheEntryOptions
			{
				Expiration = TimeSpan.FromMinutes(10),
				LocalCacheExpiration = TimeSpan.FromMinutes(2)
			}, 
			cancellationToken: ct);

	public async Task<List<SkyblockItemDto>> GetAllItemsAsync(CancellationToken ct, string? source = null)
	{
		var query = context.SkyblockItems.AsQueryable().AsNoTracking();
		if (!string.IsNullOrEmpty(source))
		{
			query = query.Where(i => i.Source == source);
		}
		return await query.SelectDto().ToListAsync(ct);
	}
}