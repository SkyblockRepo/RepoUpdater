using RepoAPI.Data;
using RepoAPI.Features.Pets.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace RepoAPI.Features.Pets.Services;

[RegisterService<PetService>(LifeTime.Scoped)]
public class PetService(DataContext context, HybridCache cache)
{
	public ValueTask<SkyblockPet?> GetPetByIdAsync(string id, CancellationToken ct) =>
		cache.GetOrCreateAsync(
			$"item-id-{id}",
			async c =>
			{
				return await context.SkyblockPets.FirstOrDefaultAsync(s => s.InternalId == id, c);
			},
			options: new HybridCacheEntryOptions
			{
				Expiration = TimeSpan.FromMinutes(10),
				LocalCacheExpiration = TimeSpan.FromMinutes(2)
			}, 
			cancellationToken: ct);

	public async Task<List<SkyblockPetDto>> GetAllPetsAsync(CancellationToken ct, string? source = null)
	{
		var query = context.SkyblockPets.AsQueryable().AsNoTracking();
		if (!string.IsNullOrEmpty(source))
		{
			query = query.Where(i => i.Source == source);
		}
		return await query.SelectDto().ToListAsync(ct);
	}
}