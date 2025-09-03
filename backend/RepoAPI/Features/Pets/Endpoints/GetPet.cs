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

internal class GetPetEndpoint(IPetService itemService) : Endpoint<GetPetRequest, GetPetResponse>
{
	public override void Configure()
	{
		Get("pets/{id}");
		AllowAnonymous();
		
		Summary(s => {
			s.Summary = "Get Pet by ID";
			s.Description = "Retrieves the details of a specific pet using its internal skyblock id.";
		});
		
		ResponseCache(30);
		Options(o => {
			o.CacheOutput(c => c.Expire(TimeSpan.FromSeconds(30)));
		});
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