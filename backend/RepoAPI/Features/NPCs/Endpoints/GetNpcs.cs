using RepoAPI.Features.NPCs.Models;
using RepoAPI.Features.NPCs.Services;

namespace RepoAPI.Features.NPCs.Endpoints;

internal class GetNpcsRequest
{
	[QueryParam]
	public string? Source { get; set; }
}

internal class GetNpcsResponse
{
	public Dictionary<string, SkyblockNpcDto> Npcs { get; set; } = new();
}

internal class GetNpcsEndpoint(INpcService npcService) : Endpoint<GetNpcsRequest, GetNpcsResponse>
{
	public override void Configure()
	{
		Get("npcs");
		AllowAnonymous();
		
		Summary(s => {
			s.Summary = "Get Npcs";
			s.Description = "Retrieves the details of all npcs.";
		});
		
		ResponseCache(30);
		Options(o => {
			o.CacheOutput(c => c.Expire(TimeSpan.FromSeconds(30)));
		});
	}

	public override async Task HandleAsync(GetNpcsRequest request, CancellationToken ct)
	{
		var items = await npcService.GetAllNpcsAsync(ct, request.Source);

		var result = new GetNpcsResponse
		{
			Npcs = items.ToDictionary(i => i.InternalId, i => i)
		};

		await Send.OkAsync(result, ct);
	}
}