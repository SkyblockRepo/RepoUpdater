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
	public SkyblockItemDto? Item { get; set; }
	public ItemTemplateDto? Template { get; set; }
}

internal class GetItemEndpoint(IItemService itemService, WikiDataService dataService) : Endpoint<GetItemRequest, GetItemResponse>
{
	public override void Configure()
	{
		Get("items/{id}");
		AllowAnonymous();
		
		Summary(s => {
			s.Summary = "Get Item by ID";
			s.Description = "Retrieves the details of a specific item using its internal skyblock id.";
		});
		
		ResponseCache(30);
		Options(o => {
			o.CacheOutput(c => c.Expire(TimeSpan.FromSeconds(30)));
		});
	}

	public override async Task HandleAsync(GetItemRequest req, CancellationToken ct)
	{
		var item = await itemService.GetItemByIdAsync(req.Id, ct);

		await Send.OkAsync(new GetItemResponse
		{
			Item = item,
		}, ct);
	}
}