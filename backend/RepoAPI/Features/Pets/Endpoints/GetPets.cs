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
		
		Description(b => b
			.WithTags("Pets")
			.Produces<GetItemResponse>(200)
			.Produces(404)
			.WithSummary("Get All Pets")
			.WithDescription("Get the entire list of items"));
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