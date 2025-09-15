using Microsoft.EntityFrameworkCore;
using RepoAPI.Data;
using Microsoft.Extensions.Caching.Hybrid;
using RepoAPI.Features.Zones.Models;

namespace RepoAPI.Features.Zones.Services;

public interface IZoneService
{
	ValueTask<SkyblockZone?> GetZoneByIdAsync(string id, CancellationToken ct);
	Task<List<SkyblockZoneDto>> GetAllZonesAsync(CancellationToken ct, string? source = null);
}

[RegisterService<IZoneService>(LifeTime.Scoped)]
public class ZoneService(DataContext context, HybridCache cache) : IZoneService
{
	public ValueTask<SkyblockZone?> GetZoneByIdAsync(string id, CancellationToken ct) =>
		cache.GetOrCreateAsync(
			$"zone-id-{id}",
			async c =>
			{
				return await context.SkyblockZones.FirstOrDefaultAsync(s => s.InternalId == id, c);
			},
			options: new HybridCacheEntryOptions
			{
				Expiration = TimeSpan.FromMinutes(10),
				LocalCacheExpiration = TimeSpan.FromMinutes(2)
			}, 
			cancellationToken: ct);

	public async Task<List<SkyblockZoneDto>> GetAllZonesAsync(CancellationToken ct, string? source = null)
	{
		var query = context.SkyblockZones.AsQueryable().AsNoTracking();
		if (!string.IsNullOrEmpty(source))
		{
			query = query.Where(i => i.Source == source);
		}
		return await query.Select(pet => pet.ToDto()).ToListAsync(ct);
	}
}