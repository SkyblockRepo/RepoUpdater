using RepoAPI.Features.Shops.Models;
using RepoAPI.Features.Shops.Services;

namespace RepoAPI.Features.Shops.Endpoints;

internal class GetShopsRequest
{
	[QueryParam]
	public string? Source { get; set; }
}

internal class GetShopsResponse
{
	public Dictionary<string, SkyblockShopDto> Shops { get; set; } = new();
}

internal class GetShopsEndpoint(IShopService service) : Endpoint<GetShopsRequest, GetShopsResponse>
{
	public override void Configure()
	{
		Get("shops");
		AllowAnonymous();
		
		Summary(s => {
			s.Summary = "Get Shops";
			s.Description = "Retrieves the details of all shops.";
		});
		
		// ResponseCache(30);
		// Options(o => {
		// 	o.CacheOutput(c => c.Expire(TimeSpan.FromSeconds(30)));
		// });
	}

	public override async Task HandleAsync(GetShopsRequest request, CancellationToken ct)
	{
		var items = await service.GetAllShopsAsync(ct, request.Source);

		var result = new GetShopsResponse
		{
			Shops = items.ToDictionary(i => i.InternalId, i => i)
		};

		await Send.OkAsync(result, ct);
	}
}