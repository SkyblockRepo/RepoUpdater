using RepoAPI.Features.NPCs.Models;
using RepoAPI.Features.NPCs.Services;

namespace RepoAPI.Features.NPCs.Endpoints;

internal class GetNpcRequest
{
	[QueryParam]
	public string? Source { get; set; }
}

internal class GetNpcResponse
{
	public Dictionary<string, SkyblockNpcDto> Npcs { get; set; } = new();
}

internal class GetNpcsEndpoint(INpcService npcService) : Endpoint<GetNpcRequest, GetNpcResponse>
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

	public override async Task HandleAsync(GetNpcRequest request, CancellationToken ct)
	{
		var items = await npcService.GetAllNpcsAsync(ct, request.Source);

		var result = new GetNpcResponse
		{
			Npcs = items.ToDictionary(i => i.InternalId, i => i)
		};

		await Send.OkAsync(result, ct);
	}
}