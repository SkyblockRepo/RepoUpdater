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

internal class GetItemsEndpoint(IItemService itemService, IWikiDataService dataService) : Endpoint<GetItemsRequest, GetItemsResponse>
{
	public override void Configure()
	{
		Get("items");
		AllowAnonymous();
		
		Summary(s => {
			s.Summary = "Get Items";
			s.Description = "Retrieves the details of all items.";
		});
		
		ResponseCache(30);
		Options(o => {
			o.CacheOutput(c => c.Expire(TimeSpan.FromSeconds(30)));
		});
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