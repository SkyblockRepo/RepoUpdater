using Microsoft.EntityFrameworkCore;
using RepoAPI.Data;
using Microsoft.Extensions.Caching.Hybrid;
using RepoAPI.Features.NPCs.Models;

namespace RepoAPI.Features.NPCs.Services;

public interface INpcService
{
	ValueTask<SkyblockNpc?> GetNpcByIdAsync(string id, CancellationToken ct);
	Task<List<SkyblockNpcDto>> GetAllNpcsAsync(CancellationToken ct, string? source = null);
}

[RegisterService<INpcService>(LifeTime.Scoped)]
public class NpcService(DataContext context, HybridCache cache) : INpcService
{
	public ValueTask<SkyblockNpc?> GetNpcByIdAsync(string id, CancellationToken ct) =>
		cache.GetOrCreateAsync(
			$"item-id-{id}",
			async c =>
			{
				return await context.SkyblockNpcs.FirstOrDefaultAsync(s => s.InternalId == id, c);
			},
			options: new HybridCacheEntryOptions
			{
				Expiration = TimeSpan.FromMinutes(10),
				LocalCacheExpiration = TimeSpan.FromMinutes(2)
			}, 
			cancellationToken: ct);

	public async Task<List<SkyblockNpcDto>> GetAllNpcsAsync(CancellationToken ct, string? source = null)
	{
		var query = context.SkyblockNpcs.AsQueryable().AsNoTracking();
		if (!string.IsNullOrEmpty(source))
		{
			query = query.Where(i => i.Source == source);
		}
		return await query.Select(pet => pet.ToDto()).ToListAsync(ct);
	}
}