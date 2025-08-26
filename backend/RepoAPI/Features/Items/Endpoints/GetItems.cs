using RepoAPI.Features.Items.Models;
using RepoAPI.Features.Items.Services;
using RepoAPI.Features.Wiki.Services;

namespace RepoAPI.Features.Items.Endpoints;

internal class GetItemsRequest
{
	[QueryParam]
	public string? Source { get; set; }
}

internal class GetItemsResponse
{
	public Dictionary<string, SkyblockItemDto> Items { get; set; } = new();
}

internal class GetItemsEndpoint(ItemService itemService, WikiDataService dataService) : Endpoint<GetItemsRequest, GetItemsResponse>
{
	public override void Configure()
	{
		Get("items");
		AllowAnonymous();
		
		Description(b => b
			.WithTags("Items")
			.Produces<GetItemResponse>(200)
			.Produces(404)
			.WithSummary("Get All Items")
			.WithDescription("Get the entire list of items"));
	}

	public override async Task HandleAsync(GetItemsRequest request, CancellationToken ct)
	{
		var items = await itemService.GetAllItemsAsync(ct, request.Source);

		var result = new GetItemsResponse
		{
			Items = items.ToDictionary(i => i.InternalId, i => i)
		};

		await Send.OkAsync(result, ct);
	}
}