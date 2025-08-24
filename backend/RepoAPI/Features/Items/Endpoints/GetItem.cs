using RepoAPI.Features.Items.Models;
using RepoAPI.Features.Items.Services;
using RepoAPI.Features.Wiki.Services;
using RepoAPI.Features.Wiki.Templates.ItemTemplate;

namespace RepoAPI.Features.Items.Endpoints;

internal class GetItemRequest
{
	public string Id { get; set; }	
}

internal class GetItemResponse
{
	public SkyblockItem? Item { get; set; }
	public ItemTemplateDto? Template { get; set; }
}

internal class GetItemEndpoint(ItemService itemService, WikiDataService dataService) : Endpoint<GetItemRequest, GetItemResponse>
{
	public override void Configure()
	{
		Get("items/{id}");
		AllowAnonymous();
		
		Description(b => b
			.WithTags("Items")
			.Produces<GetItemResponse>(200)
			.Produces(404)
			.WithSummary("Get Item by ID")
			.WithDescription("Get an item by its ID"));
	}

	public override async Task HandleAsync(GetItemRequest req, CancellationToken ct)
	{
		var item = await itemService.GetItemByIdAsync(req.Id, ct);
		if (item is null)
		{
			await Send.NotFoundAsync(ct);
			return;
		}
		
		var template = await dataService.GetItemData(item.ItemId);

		await Send.OkAsync(new GetItemResponse
		{
			Item = item,
			Template = template
		}, ct);
	}
}