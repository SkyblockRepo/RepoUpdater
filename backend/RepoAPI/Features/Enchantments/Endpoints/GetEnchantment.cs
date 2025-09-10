using Microsoft.EntityFrameworkCore;
using RepoAPI.Data;
using RepoAPI.Features.Enchantments.Models;

namespace RepoAPI.Features.Enchantments.Endpoints;

internal class GetItemRequest
{
	public string Id { get; set; }	
}

internal class GetItemResponse
{
	public SkyblockEnchantmentDto? Enchantment { get; set; }
}

internal class GetEnchantmentEndpoint(DataContext context) : Endpoint<GetItemRequest, GetItemResponse>
{
	public override void Configure()
	{
		Get("enchantments/{id}");
		AllowAnonymous();
		
		Summary(s => {
			s.Summary = "Get Enchantment by ID";
			s.Description = "Retrieves the details of a specific enchantment using its internal skyblock id.";
		});
		
		ResponseCache(30);
		Options(o => {
			o.CacheOutput(c => c.Expire(TimeSpan.FromSeconds(30)));
		});
	}

	public override async Task HandleAsync(GetItemRequest req, CancellationToken ct)
	{
		var enchantment = await context.SkyblockEnchantments.FirstOrDefaultAsync(e => e.InternalId == req.Id, ct);
		
		var result = new GetItemResponse
		{
			Enchantment = enchantment?.ToDto()
		};
		
		await Send.OkAsync(result, ct);
	}
}