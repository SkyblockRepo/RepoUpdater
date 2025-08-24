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
				return await context.SkyblockItems.FirstOrDefaultAsync(s => s.ItemId == id, c);
			},
			options: new HybridCacheEntryOptions
			{
				Expiration = TimeSpan.FromMinutes(10),
				LocalCacheExpiration = TimeSpan.FromMinutes(2)
			}, 
			cancellationToken: ct);
}