using RepoAPI.Features.Shops.Models;
using RepoAPI.Features.Shops.Services;

namespace RepoAPI.Features.Shops.Endpoints;

internal class GetShopRequest
{
	public string Id { get; set; }	
}

internal class GetShopResponse
{
	public SkyblockShopDto? Shop { get; set; }
}

internal class GetShopEndpoint(IShopService service) : Endpoint<GetShopRequest, GetShopResponse>
{
	public override void Configure()
	{
		Get("shops/{id}");
		AllowAnonymous();
		
		Summary(s => {
			s.Summary = "Get Shop by ID";
			s.Description = "Retrieves the details of a specific shop using its internal skyblock id.";
		});
		
		// ResponseCache(30);
		// Options(o => {
		// 	o.CacheOutput(c => c.Expire(TimeSpan.FromSeconds(30)));
		// });
	}

	public override async Task HandleAsync(GetShopRequest req, CancellationToken ct)
	{
		var npc = await service.GetShopByIdAsync(req.Id, ct);
		
		if (npc is null) {
			await Send.NotFoundAsync(ct);
			return;
		}

		await Send.OkAsync(new GetShopResponse
		{
			Shop = npc.ToDto()
		}, ct);
	}
}