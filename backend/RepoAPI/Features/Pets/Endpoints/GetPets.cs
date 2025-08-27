using RepoAPI.Features.Items.Endpoints;
using RepoAPI.Features.Items.Services;
using RepoAPI.Features.Pets.Models;
using RepoAPI.Features.Pets.Services;
using RepoAPI.Features.Wiki.Services;

namespace RepoAPI.Features.Pets.Endpoints;

internal class GetPetsRequest
{
	[QueryParam]
	public string? Source { get; set; }
}

internal class GetPetsResponse
{
	public Dictionary<string, SkyblockPetDto> Pets { get; set; } = new();
}

internal class GetPetsEndpoint(PetService petService) : Endpoint<GetPetsRequest, GetPetsResponse>
{
	public override void Configure()
	{
		Get("pets");
		AllowAnonymous();
		
		Summary(s => {
			s.Summary = "Get Pets";
			s.Description = "Retrieves the details of a all pets.";
		});
		
		ResponseCache(30);
		Options(o => {
			o.CacheOutput(c => c.Expire(TimeSpan.FromSeconds(30)));
		});
	}

	public override async Task HandleAsync(GetPetsRequest request, CancellationToken ct)
	{
		var items = await petService.GetAllPetsAsync(ct, request.Source);

		var result = new GetPetsResponse
		{
			Pets = items.ToDictionary(i => i.InternalId, i => i)
		};

		await Send.OkAsync(result, ct);
	}
}