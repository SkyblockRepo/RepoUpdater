using RepoAPI.Features.Zones.Models;
using RepoAPI.Features.Zones.Services;

namespace RepoAPI.Features.Zones.Endpoints;

internal class GetZonesRequest
{
	[QueryParam]
	public string? Source { get; set; }
}

internal class GetZonesResponse
{
	public Dictionary<string, SkyblockZoneDto> Zones { get; set; } = new();
}

internal class GetZonesEndpoint(IZoneService service) : Endpoint<GetZonesRequest, GetZonesResponse>
{
	public override void Configure()
	{
		Get("zones");
		AllowAnonymous();
		
		Summary(s => {
			s.Summary = "Get Zones";
			s.Description = "Retrieves the details of all zones.";
		});
		
		ResponseCache(30);
		Options(o => {
			o.CacheOutput(c => c.Expire(TimeSpan.FromSeconds(30)));
		});
	}

	public override async Task HandleAsync(GetZonesRequest request, CancellationToken ct)
	{
		var items = await service.GetAllZonesAsync(ct, request.Source);

		var result = new GetZonesResponse
		{
			Zones = items.ToDictionary(i => i.InternalId, i => i)
		};

		await Send.OkAsync(result, ct);
	}
}