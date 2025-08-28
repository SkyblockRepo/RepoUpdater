using Microsoft.EntityFrameworkCore;
using RepoAPI.Data;
using RepoAPI.Features.Enchantments.Models;

namespace RepoAPI.Features.Enchantments.Endpoints;

internal class GetEnchantmentsRequest
{
	[QueryParam]
	public string? Source { get; set; }
}

internal class GetEnchantmentsResponse
{
	public Dictionary<string, SkyblockEnchantment> Enchantments { get; set; } = new();
}

internal class GetEnchantmentsEndpoint(DataContext context) : Endpoint<GetEnchantmentsRequest, GetEnchantmentsResponse>
{
	public override void Configure()
	{
		Get("enchantments");
		AllowAnonymous();
		
		Summary(s => {
			s.Summary = "Get Enchantments";
			s.Description = "Retrieves the details of all enchantments.";
		});
		
		ResponseCache(30);
		Options(o => {
			o.CacheOutput(c => c.Expire(TimeSpan.FromSeconds(30)));
		});
	}

	public override async Task HandleAsync(GetEnchantmentsRequest request, CancellationToken ct)
	{
		var query = context.SkyblockEnchantments
			.AsNoTracking();
			
		if (!string.IsNullOrEmpty(request.Source))
		{
			query = query.Where(i => i.Source == request.Source);
		}
			
		var enchantments= await query.ToListAsync(ct);

		var result = new GetEnchantmentsResponse
		{
			Enchantments = enchantments.ToDictionary(i => i.InternalId, i => i)
		};

		await Send.OkAsync(result, ct);
	}
}