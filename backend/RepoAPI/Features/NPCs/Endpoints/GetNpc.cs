using RepoAPI.Features.NPCs.Models;
using RepoAPI.Features.NPCs.Services;

namespace RepoAPI.Features.Npcs.Endpoints;

internal class GetNpcRequest
{
	public string Id { get; set; }	
}

internal class GetNpcResponse
{
	public SkyblockNpcDto? Npc { get; set; }
}

internal class GetNpcEndpoint(INpcService itemService) : Endpoint<GetNpcRequest, GetNpcResponse>
{
	public override void Configure()
	{
		Get("npcs/{id}");
		AllowAnonymous();
		
		Summary(s => {
			s.Summary = "Get NPC by ID";
			s.Description = "Retrieves the details of a specific npc using its internal skyblock id.";
		});
		
		ResponseCache(30);
		Options(o => {
			o.CacheOutput(c => c.Expire(TimeSpan.FromSeconds(30)));
		});
	}

	public override async Task HandleAsync(GetNpcRequest req, CancellationToken ct)
	{
		var npc = await itemService.GetNpcByIdAsync(req.Id, ct);
		
		if (npc is null) {
			await Send.NotFoundAsync(ct);
			return;
		}

		await Send.OkAsync(new GetNpcResponse
		{
			Npc = npc.ToDto()
		}, ct);
	}
}