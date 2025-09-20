using Microsoft.EntityFrameworkCore;
using RepoAPI.Data;
using Microsoft.Extensions.Caching.Hybrid;
using RepoAPI.Features.Shops.Models;

namespace RepoAPI.Features.Shops.Services;

public interface IShopService
{
	ValueTask<SkyblockShop?> GetShopByIdAsync(string id, CancellationToken ct);
	Task<List<SkyblockShopDto>> GetAllShopsAsync(CancellationToken ct, string? source = null);
}

[RegisterService<IShopService>(LifeTime.Scoped)]
public class ShopService(DataContext context, HybridCache cache) : IShopService
{
	public ValueTask<SkyblockShop?> GetShopByIdAsync(string id, CancellationToken ct) =>
		cache.GetOrCreateAsync(
			$"shop-id-{id}",
			async c =>
			{
				return await context.SkyblockShops.FirstOrDefaultAsync(s => s.InternalId == id, c);
			},
			options: new HybridCacheEntryOptions
			{
				Expiration = TimeSpan.FromMinutes(10),
				LocalCacheExpiration = TimeSpan.FromMinutes(2)
			}, 
			cancellationToken: ct);

	public async Task<List<SkyblockShopDto>> GetAllShopsAsync(CancellationToken ct, string? source = null)
	{
		var query = context.SkyblockShops.AsQueryable().AsNoTracking();
		if (!string.IsNullOrEmpty(source))
		{
			query = query.Where(i => i.Source == source);
		}
		return await query.Select(pet => pet.ToDto()).ToListAsync(ct);
	}
}