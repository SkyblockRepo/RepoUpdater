using RepoAPI.Features.Zones.Models;
using RepoAPI.Features.Zones.Services;

namespace RepoAPI.Features.Zones.Endpoints;

internal class GetZoneRequest
{
	public string Id { get; set; }	
}

internal class GetZoneResponse
{
	public SkyblockZoneDto? Zone { get; set; }
}

internal class GetZoneEndpoint(IZoneService service) : Endpoint<GetZoneRequest, GetZoneResponse>
{
	public override void Configure()
	{
		Get("zones/{id}");
		AllowAnonymous();
		
		Summary(s => {
			s.Summary = "Get Zone by ID";
			s.Description = "Retrieves the details of a specific zone using its internal skyblock id.";
		});
		
		ResponseCache(30);
		Options(o => {
			o.CacheOutput(c => c.Expire(TimeSpan.FromSeconds(30)));
		});
	}

	public override async Task HandleAsync(GetZoneRequest req, CancellationToken ct)
	{
		var npc = await service.GetZoneByIdAsync(req.Id, ct);
		
		if (npc is null) {
			await Send.NotFoundAsync(ct);
			return;
		}

		await Send.OkAsync(new GetZoneResponse
		{
			Zone = npc.ToDto()
		}, ct);
	}
}