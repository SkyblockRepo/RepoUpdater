using RepoAPI.Features.Pets.Models;
using RepoAPI.Features.Pets.Services;

namespace RepoAPI.Features.Pets.Endpoints;

internal class GetPetRequest
{
	public string Id { get; set; }	
}

internal class GetPetResponse
{
	public SkyblockPet? Pet { get; set; }
}

internal class GetPetEndpoint(PetService itemService) : Endpoint<GetPetRequest, GetPetResponse>
{
	public override void Configure()
	{
		Get("pets/{id}");
		AllowAnonymous();
		
		Description(b => b
			.WithTags("Pets")
			.Produces<GetPetResponse>(200)
			.Produces(404)
			.WithSummary("Get Pet by ID")
			.WithDescription("Get an item by its ID"));
	}

	public override async Task HandleAsync(GetPetRequest req, CancellationToken ct)
	{
		var item = await itemService.GetPetByIdAsync(req.Id, ct);

		await Send.OkAsync(new GetPetResponse
		{
			Pet = item
		}, ct);
	}
}